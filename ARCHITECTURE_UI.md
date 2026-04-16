# UI Architecture - TileTextureGenerator

## Overview

This document describes the **presentation layer architecture** (.NET MAUI) of TileTextureGenerator. It complements the main [`ARCHITECTURE.md`](./ARCHITECTURE.md) document by focusing on UI patterns, MVVM binding, and handling polymorphic ProjectBase in the user interface.

---

## Core Problem

### Business Context
The application must support editing **different project types** (FloorTileProject, WallTileProject, future types) with:
- **Common part**: `ProjectBase` properties (Name, Status, DisplayImage, Transformations) → identical for all
- **Specific part**: Concrete properties (TileShape, SourceImage, WallHeight, etc.) → **different per type**

### Hexagonal Architecture Constraints
- ❌ Core entities **must NOT know the UI** (no `[Display]` attributes, no `INotifyPropertyChanged`)
- ❌ Core entities **are NOT observable** (violates business responsibility)
- ✅ Strict separation: Presentation ↔ Adapters ↔ Core (unidirectional dependencies)
- ✅ **NO direct Core reference in Presentation.UI** (achieved via JSON contract)

---

## Architectural Decision: JSON-based + Dynamic Templates

### Chosen Approach ⭐

**Pattern**: **JSON Contract + TemplatedContentView + Convention-based ViewModels**

**Principles**:
1. **Hexagonal Decoupling**: Presentation.UI does NOT reference Core
2. **JSON as Contract**: `EditProjectUseCase` exposes project properties as `JsonObject`
3. **Shared Reference**: Same `JsonObject` instance shared between `EditProjectViewModel` and template ViewModels
4. **Dynamic Template Selection**: `TemplatedContentView` selects template by `ConcreteTypeName` (string)
5. **Convention-based Instantiation**: ViewModels auto-discovered by naming convention

---

## File Structure

```
TileTextureGenerator.Presentation.UI/
├─ Pages/
│  ├─ ManageProjectListPage.xaml             # Project list
│  └─ EditProjectPage.xaml                   # Edit (common parts + dynamic template)
│
├─ Templates/                                 # Type-specific templates
│  ├─ FloorTileTemplate.xaml
│  ├─ FloorTileTemplate.xaml.cs
│  ├─ PlaceholderTemplate.xaml               # Fallback for non-implemented types
│  └─ PlaceholderTemplate.xaml.cs
│
├─ ViewModels/
│  ├─ ManageProjectListViewModel.cs          # List coordination
│  ├─ EditProjectViewModel.cs                # Edit coordination (generic)
│  └─ FloorTileProjectViewModel.cs           # Specific template ViewModel
│
├─ Controls/
│  └─ TemplatedContentView.cs                # Dynamic template instantiation
│
├─ Selectors/
│  └─ ProjectTemplateSelector.cs             # TypeName → DataTemplate mapping
│
└─ Services/
   └─ TileShapeLocalizer.cs                  # Enum string → localized name
```

---

## Detailed Architecture

### 1. Data Flow: Core → JSON → UI

```
┌─────────────────────────────────────────────┐
│  Core.Entities.FloorTileProject            │
│  - TileShape: TileShape.Full (enum)        │
│  - SourceImage: ImageData { Bytes: [...] } │
└───────────────┬─────────────────────────────┘
                │ Serialized by
                ↓
┌─────────────────────────────────────────────┐
│  Adapters.UseCases.EditProjectUseCase      │
│  GetPropertiesJson() →                      │
│  {                                          │
│    "TileShape": "Full",       ← String!    │
│    "SourceImage": {                         │
│      "Bytes": "iVBORw0KGg..." ← base64     │
│    }                                        │
│  }                                          │
└───────────────┬─────────────────────────────┘
                │ Shared reference
                ↓
┌─────────────────────────────────────────────┐
│  Presentation.UI.ViewModels                 │
│  EditProjectViewModel                       │
│    └─ _propertiesJson (shared)             │
│         └─ FloorTileProjectViewModel        │
│              └─ reads/writes _propertiesJson│
└─────────────────────────────────────────────┘
```

**Key Benefit**: UI modifies JSON → automatically reflected in Core on Save (shared reference).

---

### 2. EditProjectPage Structure

```
┌────────────────────────────────────────────┐
│  EditProjectPage.xaml                      │
├────────────────────────────────────────────┤
│  1. Project Properties (Dynamic Template)  │
│     ┌──────────────────────────────────┐  │
│     │ TemplatedContentView             │  │
│     │  - TypeName: "FloorTileProject"  │  │
│     │  - TemplateData: FloorTileVM     │  │
│     │  - Selector: ProjectTemplateS... │  │
│     └──────────────────────────────────┘  │
│        │                                   │
│        ├─ IF FloorTileProject             │
│        │   → FloorTileTemplate.xaml       │
│        │                                   │
│        └─ ELSE                             │
│            → PlaceholderTemplate.xaml     │
├────────────────────────────────────────────┤
│  2. Save Button                            │
│     - Calls EditViewModel.SaveAsync()     │
│     - Updates Core from JSON               │
├────────────────────────────────────────────┤
│  3. Transformations (Collapsible Picker)   │
│     - Add/Remove transformations           │
└────────────────────────────────────────────┘
```

**XAML Extract** (simplified):
```xaml
<ContentPage xmlns:controls="clr-namespace:...Controls"
             xmlns:selectors="clr-namespace:...Selectors"
             x:DataType="vm:EditProjectViewModel">

    <ContentPage.Resources>
        <ResourceDictionary>
            <!-- Template selector (templates registered in C# constructor) -->
            <selectors:ProjectTemplateSelector x:Key="ProjectTemplateSelector" />
        </ResourceDictionary>
    </ContentPage.Resources>

    <Frame>
        <controls:TemplatedContentView 
            TemplateSelector="{StaticResource ProjectTemplateSelector}"
            TypeName="{Binding ConcreteTypeName}"
            TemplateData="{Binding ProjectViewModel}" />
    </Frame>
</ContentPage>
```

---

### 3. TemplatedContentView (Custom Control)

**Responsibility**: Dynamically select and instantiate the correct template based on `ConcreteTypeName`.

**Code**:
```csharp
public class TemplatedContentView : ContentView
{
    public ProjectTemplateSelector? TemplateSelector { get; set; }
    public string? TypeName { get; set; }
    public object? TemplateData { get; set; }

    private void UpdateContent()
    {
        Content = null;

        if (TemplateSelector == null || string.IsNullOrEmpty(TypeName))
            return;

        var template = TemplateSelector.SelectTemplateByTypeName(TypeName);

        if (template != null)
        {
            var view = (View)template.CreateContent();
            view.BindingContext = TemplateData;
            Content = view;
        }
        else
        {
            // No specific template found: use placeholder
            Content = new PlaceholderTemplate();
        }
    }
}
```

**Benefits**:
- ✅ Template instantiated **only if used** (memory efficient)
- ✅ Automatic fallback to `PlaceholderTemplate`
- ✅ No hardcoded type checks

---

### 4. ProjectTemplateSelector (Centralized Registration)

**Responsibility**: Map `ConcreteTypeName` (string) to `DataTemplate` and `ViewModel Type` using explicit registration.

**Code**:
```csharp
/// <summary>
/// Holds template and ViewModel type information for a concrete project type.
/// </summary>
public record ProjectTemplateInfo(DataTemplate Template, Type ViewModelType);

public class ProjectTemplateSelector : DataTemplateSelector
{
    private readonly Dictionary<string, ProjectTemplateInfo> _projectTemplates = new();

    /// <summary>
    /// Constructor: Register all project templates here.
    /// </summary>
    public ProjectTemplateSelector()
    {
        // Register FloorTileProject
        RegisterTemplate("FloorTileProject", typeof(FloorTileTemplate), typeof(FloorTileProjectViewModel));

        // To add WallTileProject, uncomment:
        // RegisterTemplate("WallTileProject", typeof(WallTileTemplate), typeof(WallTileProjectViewModel));
    }

    /// <summary>
    /// Registers a project template with its associated ViewModel type.
    /// </summary>
    private void RegisterTemplate(string projectTypeName, Type templateType, Type viewModelType)
    {
        var dataTemplate = new DataTemplate(templateType);
        _projectTemplates[projectTypeName] = new ProjectTemplateInfo(dataTemplate, viewModelType);
    }

    /// <summary>
    /// Gets the list of registered project type names.
    /// Useful for testing to verify all concrete project types are registered.
    /// </summary>
    public IReadOnlyCollection<string> RegisteredProjectTypes => _projectTemplates.Keys;

    public DataTemplate? SelectTemplateByTypeName(string concreteTypeName)
    {
        return _projectTemplates.TryGetValue(concreteTypeName, out var info) ? info.Template : null;
    }

    public Type? GetViewModelType(string concreteTypeName)
    {
        return _projectTemplates.TryGetValue(concreteTypeName, out var info) ? info.ViewModelType : null;
    }
}
```

**Benefits**:
- ✅ **Single file to modify**: Add one line in constructor for new project types
- ✅ **No XAML changes**: Templates registered in C# only
- ✅ **Atomic storage**: Template + ViewModel stored together
- ✅ **Testable**: `RegisteredProjectTypes` property for integrity tests

---

### 5. FloorTileProjectViewModel (Unified Constructor)

**Responsibility**: Provide observable properties for `FloorTileTemplate.xaml`.

**Unified Constructor Signature** (all template ViewModels):
```csharp
public FloorTileProjectViewModel(
    JsonObject propertiesJson,           // Shared JSON reference
    EditProjectViewModel parentViewModel // Provides services
)
```

**Code Extract**:
```csharp
public class FloorTileProjectViewModel : INotifyPropertyChanged
{
    private readonly JsonObject _propertiesJson;
    private readonly TileShapeLocalizer _tileShapeLocalizer;

    public FloorTileProjectViewModel(
        JsonObject propertiesJson, 
        EditProjectViewModel parentViewModel)
    {
        _propertiesJson = propertiesJson;
        _tileShapeLocalizer = parentViewModel.TileShapeLocalizer;

        // Load TileShape from JSON (string value like "Full")
        var tileShapeValue = _propertiesJson["TileShape"]?.GetValue<string>() ?? "Full";
        SelectedTileShape = AvailableTileShapes.FirstOrDefault(x => x.Value == tileShapeValue);
    }

    public TileShapeItem? SelectedTileShape
    {
        get => _selectedTileShape;
        set
        {
            if (SetProperty(ref _selectedTileShape, value))
            {
                // Update JSON (shared reference!)
                if (value != null)
                    _propertiesJson["TileShape"] = value.Value; // String, not enum!
            }
        }
    }
}
```

**Key Points**:
- ✅ Works with `JsonObject` (not Core entities)
- ✅ Enums stored as **strings** in JSON ("Full" not 0)
- ✅ Shared reference: modifications auto-reflected

---

### 6. EditProjectViewModel (Generic Coordination)

**Responsibility**: Orchestrate the editing workflow (100% generic, no type-specific code).

**Key Code**:
```csharp
public class EditProjectViewModel : INotifyPropertyChanged
{
    private readonly JsonObject _propertiesJson; // Shared with template VM

    public EditProjectViewModel(
        EditProjectUseCase editUseCase,
        TileShapeLocalizer tileShapeLocalizer, 
        /* ... other services */)
    {
        _propertiesJson = editUseCase.GetPropertiesJson(); // Get once
        _projectViewModel = CreateProjectViewModel();       // Generic!
    }

    public string ConcreteTypeName => _editUseCase.ConcreteTypeName;
    public object? ProjectViewModel => _projectViewModel;
    public TileShapeLocalizer TileShapeLocalizer => _tileShapeLocalizer;

    private object? CreateProjectViewModel()
    {
        // Convention: "FloorTileProject" → "FloorTileProjectViewModel"
        var viewModelTypeName = 
            $"TileTextureGenerator.Presentation.UI.ViewModels.{ConcreteTypeName}ViewModel";
        var viewModelType = Type.GetType(viewModelTypeName);

        if (viewModelType == null)
            return null; // PlaceholderTemplate will be used

        // Unified signature: (JsonObject, EditProjectViewModel)
        return Activator.CreateInstance(viewModelType, _propertiesJson, this);
    }

    private async Task SaveAsync()
    {
        // Generic: works for ALL project types!
        _editUseCase.UpdatePropertiesFromJson(_propertiesJson);
        await _editUseCase.SaveAsync();
    }
}
```

**Benefits**:
- ✅ **Zero type-specific code** (convention-based)
- ✅ Works for all future project types
- ✅ Shared JSON reference auto-updates Core

---

## Adding a New Project Type (Step-by-Step)

### 1. Create the Template XAML

**File**: `TileTextureGenerator.Presentation.UI/Templates/WallTileTemplate.xaml`

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             x:Class="TileTextureGenerator.Presentation.UI.Templates.WallTileTemplate">

    <VerticalStackLayout Spacing="20">
        <Label Text="Wall Height" />
        <Picker ItemsSource="{Binding AvailableHeights}"
                SelectedItem="{Binding SelectedHeight}" />
    </VerticalStackLayout>
</ContentView>
```

### 2. Create the Code-Behind

**File**: `TileTextureGenerator.Presentation.UI/Templates/WallTileTemplate.xaml.cs`

```csharp
namespace TileTextureGenerator.Presentation.UI.Templates;

public partial class WallTileTemplate : ContentView
{
    public WallTileTemplate()
    {
        InitializeComponent();
    }
}
```

### 3. Create the ViewModel

**File**: `TileTextureGenerator.Presentation.UI/ViewModels/WallTileProjectViewModel.cs`

```csharp
using System.Text.Json.Nodes;

namespace TileTextureGenerator.Presentation.UI.ViewModels;

public class WallTileProjectViewModel : INotifyPropertyChanged
{
    private readonly JsonObject _propertiesJson;

    // MANDATORY: Unified constructor signature
    public WallTileProjectViewModel(
        JsonObject propertiesJson, 
        EditProjectViewModel parentViewModel)
    {
        _propertiesJson = propertiesJson;

        // Read properties from JSON
        var heightValue = _propertiesJson["WallHeight"]?.GetValue<string>() ?? "Standard";
        // ...
    }

    // Properties bound to XAML
    public string? SelectedHeight
    {
        get => _propertiesJson["WallHeight"]?.GetValue<string>();
        set
        {
            _propertiesJson["WallHeight"] = value; // Update shared JSON
            OnPropertyChanged();
        }
    }

    // INotifyPropertyChanged implementation...
}
```

### 4. Register in ProjectTemplateSelector

**File**: `TileTextureGenerator.Presentation.UI/Selectors/ProjectTemplateSelector.cs`

**Add ONE line in the constructor**:
```csharp
public ProjectTemplateSelector()
{
    RegisterTemplate("FloorTileProject", typeof(FloorTileTemplate), typeof(FloorTileProjectViewModel));

    // ADD THIS LINE:
    RegisterTemplate("WallTileProject", typeof(WallTileTemplate), typeof(WallTileProjectViewModel));
}
```

### 5. DONE! 🎉

**That's it!** No XAML modification needed:
- ✅ `WallTileTemplate` registered with `WallTileProject` type name
- ✅ `WallTileProjectViewModel` associated automatically
- ✅ JSON contract auto-handled
- ✅ **Only 1 file modified** (`ProjectTemplateSelector.cs`)

---

## Key Conventions

| Convention | Example | Result |
|------------|---------|--------|
| **Registration in Constructor** | `RegisterTemplate("FloorTileProject", ...)` | Maps type name to template + ViewModel |
| **ViewModel Class Name** | `FloorTileProjectViewModel` | Passed to `RegisterTemplate()` |
| **ViewModel Constructor** | `(JsonObject, EditProjectViewModel)` | **MANDATORY** signature |
| **Enum Serialization** | `TileShape.Full` | Stored as `"Full"` (string in JSON) |
| **Image Serialization** | `byte[]` | Stored as base64 string |

---

## Testing Strategy

### Unit Tests for ProjectTemplateSelector

```csharp
[Fact]
public void WhenAllRegisteredProjects_ThenEachHasTemplate()
{
    // Arrange
    var selector = new ProjectTemplateSelector 
    { 
        FloorTileTemplate = new DataTemplate(typeof(FloorTileTemplate))
    };

    var registeredTypes = new[] { "FloorTileProject", "WallTileProject" };

    // Act & Assert
    foreach (var typeName in registeredTypes)
    {
        var template = selector.SelectTemplateByTypeName(typeName);
        Assert.NotNull(template); 
        // ❌ Will fail for WallTileProject (TDD: implement WallTileTemplate)
    }
}
```

---

## Benefits of this Architecture

✅ **Hexagonal Compliance**: Presentation.UI does NOT reference Core  
✅ **JSON Contract**: Stable interface between layers  
✅ **Centralized Registration**: ONE file to modify for new types (`ProjectTemplateSelector.cs`)  
✅ **Memory Efficient**: Templates instantiated only when needed  
✅ **Testable**: `RegisteredProjectTypes` property for integrity tests  
✅ **Maintainable**: Adding new project types = **3 files + 1 line**, zero XAML changes  
✅ **Enum Readability**: JSON contains `"Full"` not `0`  
✅ **Shared Reference**: UI modifications auto-reflected on Save  
✅ **No Reflection at Runtime**: Template selection uses pre-built dictionary

---

## Related Documents

- [`ARCHITECTURE.md`](./ARCHITECTURE.md) - Overall architecture
- [`ARCHITECTURE_SERIALIZATION.md`](./ARCHITECTURE_SERIALIZATION.md) - JSON persistence
- [`.github/copilot-instructions.md`](.github/copilot-instructions.md) - Development guidelines

public partial class FloorTileProjectDetailsView : ContentView
{
    public FloorTileProjectDetailsView()
    {
        InitializeComponent();
        
        // When BindingContext changes, wrap entity in ViewModel
        this.BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is FloorTileProject project)
        {
            BindingContext = new FloorTileProjectDetailsViewModel(project);
        }
    }
}
```

#### FloorTileProjectDetailsViewModel.cs
```csharp
namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// ViewModel for editing FloorTileProject-specific properties.
/// Wraps the entity with observable properties for UI binding.
/// </summary>
public class FloorTileProjectDetailsViewModel : INotifyPropertyChanged
{
    private readonly FloorTileProject _project;
    private TileShape _selectedTileShape;
    private ImageData? _selectedSourceImage;

    public FloorTileProjectDetailsViewModel(FloorTileProject project)
    {
        _project = project;
        
        // Initialize with current values
        _selectedTileShape = project.TileShape;
        _selectedSourceImage = project.SourceImage;
        
        // Populate available values for Picker
        AvailableTileShapes = Enum.GetValues<TileShape>().ToList();
    }

    // Observable properties for UI binding
    public TileShape SelectedTileShape
    {
        get => _selectedTileShape;
        set => SetProperty(ref _selectedTileShape, value);
    }

    public ImageData? SelectedSourceImage
    {
        get => _selectedSourceImage;
        set => SetProperty(ref _selectedSourceImage, value);
    }

    public List<TileShape> AvailableTileShapes { get; }

    // Apply changes to entity (called by parent ViewModel)
    public void ApplyChanges()
    {
        _project.TileShape = _selectedTileShape;
        _project.SourceImage = _selectedSourceImage;
        // Note: SaveChangesAsync() is called by EditProjectViewModel
    }

    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
```

---

### 4. EditProjectViewModel (Coordinateur)

**Responsabilité** : Coordonner la sauvegarde globale et appeler le Use Case.

```csharp
public class EditProjectViewModel : INotifyPropertyChanged
{
    private readonly ProjectBase _project;
    private readonly EditProjectUseCase _editProjectUseCase; // Use Case à créer

    public ProjectBase Project => _project;
    
    public ICommand SaveCommand { get; }
    public ICommand AddTransformationCommand { get; }
    // ... autres commands

    public EditProjectViewModel(ProjectBase project, EditProjectUseCase editProjectUseCase)
    {
        _project = project;
        _editProjectUseCase = editProjectUseCase;
        
        SaveCommand = new Command(async () => await SaveAsync());
        AddTransformationCommand = new Command<string>(async type => await AddTransformationAsync(type));
    }

    private async Task SaveAsync()
    {
        // Step 1: Retrieve ViewModel from ContentView (via Messenger or direct reference)
        // Step 2: Call ApplyChanges() on type-specific ViewModel
        // Step 3: Call entity SaveChangesAsync
        await _project.SaveChangesAsync();
    }

    private async Task AddTransformationAsync(string transformationType)
    {
        await _project.AddTransformationAsync(transformationType);
    }
}
```

---

## Flux de données complet

### Scénario : Éditer FloorTileProject

```
1. User clique "Load Project" sur ManageProjectListPage
   ↓
2. ManageProjectListViewModel
   └─ await _useCase.LoadProjectAsync("MyFloorProject")
      └─ Retourne ProjectBase (instance concrète FloorTileProject)
   ↓
3. Navigation → EditProjectPage
   └─ BindingContext = new EditProjectViewModel(project, editUseCase)
   ↓
4. MAUI détecte type concret (project is FloorTileProject)
   ↓
5. ProjectTypeTemplateSelector.OnSelectTemplate(project, ...)
   └─ Retourne FloorTileTemplate
   ↓
6. ContentView rend FloorTileProjectDetailsView.xaml
   ↓
7. FloorTileDetailsView.BindingContextChanged
   └─ Crée FloorTileProjectDetailsViewModel(project as FloorTileProject)
   ↓
8. User modifie TileShape dans Picker
   └─ Binding → ViewModel.SelectedTileShape (temporaire, observable)
   ↓
9. User clique "Validate"
   ↓
10. EditProjectViewModel.SaveAsync()
    ├─ Récupère FloorTileProjectDetailsViewModel
    ├─ Appelle viewModel.ApplyChanges()
    │  └─ project.TileShape = SelectedTileShape
    └─ await project.SaveChangesAsync()
       └─ Store.SaveAsync(project)
```

---

## Binding et observabilité

### Problème : Entités Core non observables

Les entités DDD **ne doivent pas implémenter** `INotifyPropertyChanged` :
```csharp
// ❌ Violation DDD
public class FloorTileProject : ProjectBase, INotifyPropertyChanged // NON !
{
    private TileShape _tileShape;
    public TileShape TileShape 
    { 
        get => _tileShape;
        set 
        { 
            _tileShape = value; 
            OnPropertyChanged(); // Pollution UI dans Core !
        }
    }
}
```

### Solution : ViewModels wrapper

Les **ViewModels typés** encapsulent les entités et exposent des propriétés observables :

```csharp
// ✅ Séparation respectée
public class FloorTileProjectDetailsViewModel : INotifyPropertyChanged
{
    private readonly FloorTileProject _project; // Entité pure
    private TileShape _selectedTileShape;       // Valeur temporaire observable

    public TileShape SelectedTileShape
    {
        get => _selectedTileShape;
        set => SetProperty(ref _selectedTileShape, value); // INotifyPropertyChanged
    }

    public void ApplyChanges()
    {
        _project.TileShape = _selectedTileShape; // Synchronisation à la validation
    }
}
```

**Avantages** :
- ✅ Entités Core restent pures (pas de pollution UI)
- ✅ Binding MAUI natif fonctionnel (INotifyPropertyChanged dans ViewModel)
- ✅ Validation différée (pas de save à chaque frappe)

---

## Stratégie de validation

### Pattern choisi : Validation différée (bouton "Validate")

**Principe** :
1. User édite les valeurs → stockées dans le ViewModel (propriétés temporaires)
2. User clique "Validate" → `ApplyChanges()` copie les valeurs dans l'entité
3. `project.SaveChangesAsync()` persiste

**Pourquoi pas de validation temps réel ?**
- ⚠️ Complexité accrue (wrappers observables pour chaque propriété)
- ⚠️ Sauvegarde à chaque frappe (overhead I/O)
- ✅ Validation explicite = contrôle utilisateur (undo facile)

**Alternative envisagée** : Auto-save avec debouncing (possiblement plus tard).

---

## Alternatives considérées

### ❌ Option 2 : Metadata-driven UI

**Principe** : Le Use Case analyse les propriétés via reflection et génère une liste de `PropertyEditorMetadata`.

**Exemple** :
```csharp
public record PropertyEditorMetadata(
    string PropertyName,
    string ResourceKey,           // "ProjectProperty_FloorTileProject_TileShape"
    PropertyEditorType EditorType, // TextBox, ImagePicker, EnumPicker
    object? CurrentValue
);
```

**Pourquoi rejetée ?**
- ❌ Binding complexe (wrappers génériques avec reflection)
- ❌ Perte type-safety (casting runtime)
- ❌ Perte intellisense XAML
- ❌ Complexité maintenance (code générique + converters)
- ✅ Avantage unique : 0 code UI pour nouveaux types → **ne compense pas les inconvénients**

**Conclusion** : Pour 2-5 types de projets, **DataTemplateSelector est plus pragmatique**.

---

### ❌ Option 3 : Visibility bindings

**Principe** : Toutes les propriétés dans 1 vue XAML, visibilité conditionnelle.

```xaml
<StackLayout IsVisible="{Binding IsFloorTileProject}">
    <Picker SelectedItem="{Binding Project.TileShape}" />
</StackLayout>
<StackLayout IsVisible="{Binding IsWallTileProject}">
    <Entry Text="{Binding Project.WallHeight}" />
</StackLayout>
```

**Pourquoi rejetée ?**
- ❌ **Toutes les vues créées en mémoire** (gaspillage)
- ❌ ViewModel complexe (expose toutes les propriétés de tous les types)
- ❌ Ne scale pas (illisible au-delà de 3 types)

---

## Gestion des erreurs : Type manquant

### Problème
Si un développeur :
1. Crée `PillarTileProject.cs` + l'enregistre dans `TextureProjectRegistry`
2. **Oublie** de créer le DataTemplate correspondant

→ **Runtime crash** quand l'UI charge ce type de projet.

### Solution : Test d'intégrité

**Test unitaire** dans `Presentation.UI.Tests/` :

```csharp
[Fact]
public void AllRegisteredProjectTypes_HaveCorrespondingDataTemplate()
{
    // Arrange
    var selector = new ProjectTypeTemplateSelector
    {
        FloorTileTemplate = new DataTemplate(() => new Label()),
        WallTileTemplate = new DataTemplate(() => new Label())
    };
    
    var registeredTypes = TextureProjectRegistry.GetAllRegisteredTypes();
    Assert.NotEmpty(registeredTypes);

    // Act & Assert
    foreach (var projectType in registeredTypes)
    {
        var instance = TextureProjectRegistry.Create(projectType.Name, $"Test_{projectType.Name}");
        
        // This will throw NotSupportedException if template is missing
        var template = selector.OnSelectTemplate(instance, null!);
        
        Assert.NotNull(template);
    }
}
```

**Détection** : Lors de `dotnet test` (avant commit) ✅

**Amélioration future** : Fail-fast au démarrage de l'app en mode DEBUG.

---

## Use Cases architecture

### ManageProjectListUseCase (existant)

**Responsabilité** : Gérer la **liste** des projets (CRUD collection).

**Méthodes** :
- `CreateProjectAsync(name, type)` → ProjectBase
- `ListProjectsAsync()` → List<ProjectListItemDto>
- `DeleteProjectAsync(name)`
- `ArchiveProjectAsync(name)`
- `LoadProjectTypesAsync()` → List<string>
- `ProjectExistsAsync(name)` → bool

**Pattern** : Retourne des **Results** (Success/Error) pour l'UI.

---

### EditProjectUseCase (à créer)

**Responsabilité** : Gérer l'**édition** d'un projet individuel.

**Méthodes envisagées** :
- `LoadProjectForEditAsync(name)` → ProjectBase
- `SaveProjectChangesAsync(project)` → Result
- `AddTransformationAsync(project, type)` → Result
- `RemoveTransformationAsync(project, id)` → Result
- `GetTransformationAsync(project, id)` → TransformationBase

**Débat** : Use Case nécessaire ou ViewModel appelle directement `ProjectBase` ?

**Décision recommandée** :
- **Opérations simples** (AddTransformation, Save) → ViewModel appelle **directement** `project.AddTransformationAsync()`
- **Workflows complexes** (Generate PDF avec multi-étapes) → Use Case requis

**Raison** : Éviter over-engineering pour des opérations CRUD triviales.

---

## Transformations UI

### Affichage dans EditProjectPage

**Liste statique** (même pour tous les types de projets) :

```xaml
<CollectionView ItemsSource="{Binding Project.Transformations}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <SwipeView>
                <SwipeView.RightItems>
                    <SwipeItems>
                        <SwipeItem Text="Delete" 
                                   Command="{Binding Source={RelativeSource AncestorType={x:Type vm:EditProjectViewModel}}, Path=RemoveTransformationCommand}"
                                   CommandParameter="{Binding Id}" />
                    </SwipeItems>
                </SwipeView.RightItems>
                
                <StackLayout>
                    <Image Source="{Binding Icon}" />
                    <Label Text="{Binding Type}" />
                </StackLayout>
            </SwipeView>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

**Pas de DataTemplateSelector pour les transformations** (pour l'instant) : La liste affiche juste l'icône + type.

**Édition détaillée des transformations** : Scénario futur (nécessitera probablement un autre DataTemplateSelector pour les types de transformations).

---

## Localisation

### Pattern pour les labels dynamiques

**Clé de ressource générée** : `ProjectProperty_{ConcreteClassName}_{PropertyName}`

**Exemples** :
- `ProjectProperty_FloorTileProject_TileShape` → "Forme de tuile"
- `ProjectProperty_WallTileProject_WallHeight` → "Hauteur du mur"

**Implémentation** :
```csharp
// Dans le ViewModel
public string TileShapeLabel => Localization.GetString("ProjectProperty_FloorTileProject_TileShape");
```

**Ou via Converter** :
```xaml
<Label Text="{Binding PropertyName, Converter={StaticResource PropertyLabelConverter}}" />
```

---

## Checklist pour ajouter un nouveau type de projet

Quand vous créez un nouveau type de projet (ex: `PillarTileProject`) :

### 1️⃣ Core (Entities)
- [ ] Créer `Core/Entities/ConcreteProjects/PillarTileProject.cs`
- [ ] Hériter de `ProjectBase`
- [ ] Ajouter propriétés spécifiques
- [ ] Enregistrer dans constructeur statique : `TextureProjectRegistry.RegisterType<PillarTileProject>()`

### 2️⃣ Presentation.UI (Templates)
- [ ] Créer `Templates/PillarTileTemplate.xaml`
- [ ] Créer `Templates/PillarTileTemplate.xaml.cs`
- [ ] Créer `ViewModels/PillarTileProjectViewModel.cs`

### 3️⃣ ProjectTemplateSelector (ONE LINE!)
- [ ] Ajouter dans le constructeur :
  ```csharp
  RegisterTemplate("PillarTileProject", typeof(PillarTileTemplate), typeof(PillarTileProjectViewModel));
  ```

### 4️⃣ Ressources (Localisation)
- [ ] Ajouter clés dans fichier `.resx` :
  - `ProjectProperty_PillarTileProject_Height`
  - `ProjectProperty_PillarTileProject_Width`
  - etc.

### 5️⃣ Tests
- [ ] **Exécuter** `dotnet test` pour valider l'intégrité
- [ ] Le test `AllRegisteredProjectTypes_HaveCorrespondingDataTemplate` **doit passer** ✅

**Si le test échoue** → Vérifier l'étape 3 (registration manquante).

---

## Tests d'intégrité

### Test Registry ↔ TemplateSelector

**Fichier** : `Presentation.UI.Tests/Selectors/ProjectTypeTemplateSelectorTests.cs`

**Objectif** : Garantir que tous les types enregistrés dans `TextureProjectRegistry` ont un DataTemplate correspondant.

**Stratégie** :
1. Lire `TextureProjectRegistry.GetAllRegisteredTypes()`
2. Comparer avec `ProjectTemplateSelector.RegisteredProjectTypes`
3. Vérifier qu'ils sont identiques

**Code** :
```csharp
[Fact]
public void AllRegisteredProjectTypes_HaveCorrespondingDataTemplate()
{
    // Arrange
    var selector = new ProjectTemplateSelector();
    var registeredInCore = TextureProjectRegistry.GetAllRegisteredTypes()
        .Select(t => t.Name)
        .OrderBy(n => n)
        .ToList();

    var registeredInUI = selector.RegisteredProjectTypes
        .OrderBy(n => n)
        .ToList();

    // Assert
    Assert.Equal(registeredInCore, registeredInUI);
}
```

**Détection** :
- ✅ Lors de `dotnet test` (avant commit)
- ✅ Dans CI/CD (si configuré)

**Message d'erreur** :
```
Expected: [FloorTileProject, WallTileProject]
Actual:   [FloorTileProject]
Missing template for: WallTileProject
```

---

## Décisions d'architecture justifiées

### Pourquoi DataTemplateSelector plutôt que Metadata-driven ?

| Critère | DataTemplateSelector | Metadata-driven |
|---------|---------------------|-----------------|
| **Respect DDD** | ✅ Core ignorant de l'UI | ✅ (si métadonnées dans Use Case) |
| **Type-safety** | ✅ Compile-time | ❌ Runtime (casting) |
| **Intellisense** | ✅ Complet | ❌ Limité |
| **Binding** | ✅ Natif MAUI | ⚠️ Wrapper générique |
| **Maintenance** | ✅ 1 fichier = 1 responsabilité | ⚠️ Code générique complexe |
| **Extensibilité** | ⚠️ 3 fichiers par type | ✅ 0 fichier UI |
| **Testabilité** | ✅ ViewModels isolés | ⚠️ Wrappers génériques |
| **Code pour 3 types** | ~300 lignes (3×100) | ~400 lignes (1 système complexe) |

**Conclusion** : Pour **2-5 types de projets**, DataTemplateSelector est plus **simple, maintenable et idiomatique MAUI**.

---

### Pourquoi des ViewModels typés par DataTemplate ?

**Alternative rejetée** : 1 ViewModel monolithique `EditProjectViewModel` qui expose toutes les propriétés de tous les types.

```csharp
// ❌ Anti-pattern
public class EditProjectViewModel
{
    // FloorTile properties
    public TileShape? TileShape { get; set; }
    
    // WallTile properties
    public int? WallHeight { get; set; }
    
    // PillarTile properties
    public int? PillarRadius { get; set; }
}
```

**Problèmes** :
- ❌ Propriétés nullables partout (quel type est actif ?)
- ❌ ViewModel énorme (scale mal)
- ❌ Logique conditionnelle complexe (if IsFloorTile...)

**Solution adoptée** : 1 ViewModel par type concret.

```csharp
// ✅ Pattern recommandé
public class FloorTileProjectDetailsViewModel
{
    public TileShape SelectedTileShape { get; set; } // Non-nullable, typé
}

public class WallTileProjectDetailsViewModel
{
    public int WallHeight { get; set; } // Propriétés spécifiques
}
```

**Avantages** :
- ✅ Séparation des responsabilités (SRP)
- ✅ Propriétés non-nullables (type-safety)
- ✅ Testable isolément

---

### Pourquoi validation différée (bouton "Validate") ?

**Alternative rejetée** : Auto-save à chaque changement.

```csharp
// ❌ Complexe
public TileShape SelectedTileShape
{
    get => _selectedTileShape;
    set
    {
        SetProperty(ref _selectedTileShape, value);
        _project.TileShape = value;
        _ = _project.SaveChangesAsync(); // Async fire-and-forget ⚠️
    }
}
```

**Problèmes** :
- ❌ I/O à chaque frappe (performance)
- ❌ Fire-and-forget async (erreurs silencieuses)
- ❌ Pas d'undo (changements immédiatement persistés)

**Solution adoptée** : Bouton "Validate" explicite.

**Avantages** :
- ✅ Contrôle utilisateur (peut annuler)
- ✅ 1 seule écriture disque (batch)
- ✅ Gestion d'erreur centralisée

---

## Tests UI recommandés

### 1. Test d'intégrité (OBLIGATOIRE)
**Fichier** : `Presentation.UI.Tests/Selectors/ProjectTypeTemplateSelectorTests.cs`

**Teste** : Registry ↔ TemplateSelector cohérence.

---

### 2. Tests ViewModels (recommandés)
**Exemples** :
- `FloorTileProjectDetailsViewModel.WhenTileShapeChanged_ThenPropertyChangedRaised`
- `FloorTileProjectDetailsViewModel.WhenApplyChanges_ThenProjectPropertiesUpdated`

---

### 3. Tests navigation (optionnels)
**Avec UI Testing framework** : Vérifier que Create/Load naviguent correctement vers EditProjectPage.

---

## Références

### Documents liés
- [`ARCHITECTURE.md`](./ARCHITECTURE.md) - Architecture globale (hexagonale, DDD)
- [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) - Instructions de développement

### Documentation Microsoft
- [DataTemplateSelector (MAUI)](https://learn.microsoft.com/dotnet/maui/fundamentals/datatemplate)
- [MVVM Pattern (MAUI)](https://learn.microsoft.com/dotnet/maui/xaml/fundamentals/mvvm)
- [Data Binding (MAUI)](https://learn.microsoft.com/dotnet/maui/fundamentals/data-binding/)

---

## Prochaines étapes

### À implémenter
1. ✅ `TextureProjectRegistry.GetAllRegisteredTypes()` (méthode helper)
2. ⬜ `ProjectTypeTemplateSelector.cs`
3. ⬜ `FloorTileProjectDetailsView` (triplet XAML/CS/ViewModel)
4. ⬜ `WallTileProjectDetailsView` (triplet XAML/CS/ViewModel)
5. ⬜ `EditProjectPage.xaml` (coordination)
6. ⬜ `EditProjectViewModel.cs` (coordinateur)
7. ⬜ `Presentation.UI.Tests/` (projet + test d'intégrité)

### Workflow de développement
1. Développeur crée nouveau type Core + enregistre dans Registry
2. Développeur crée triplet View/ViewModel + ajoute dans Selector
3. **`dotnet test`** → Vérifie intégrité automatiquement ✅
4. Si test vert → Commit + Push

---

## Notes importantes

- **Pas de INotifyPropertyChanged dans Core** : Les entités DDD doivent rester pures.
- **ViewModels = couche d'adaptation** : Transforment entités en objets observables pour l'UI.
- **DataTemplate avec ViewModel typé** : Pattern MAUI standard pour polymorphisme UI.
- **Test d'intégrité obligatoire** : Seul moyen de détecter type manquant avant runtime.

---

**Dernière mise à jour** : 2025-01-XX (décisions prises lors du refactoring DDD de `ProjectBase.ArchiveAsync`)
