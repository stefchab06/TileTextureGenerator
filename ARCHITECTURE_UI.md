# Architecture UI - TileTextureGenerator

## Vue d'ensemble

Ce document décrit les **décisions d'architecture pour la couche présentation** (.NET MAUI) de TileTextureGenerator. Il complète le document principal [`ARCHITECTURE.md`](./ARCHITECTURE.md) en se concentrant sur les patterns UI, le binding MVVM, et la gestion du polymorphisme ProjectBase dans l'interface utilisateur.

---

## Problématique principale

### Contexte métier
L'application doit permettre d'éditer **différents types de projets** (FloorTileProject, WallTileProject, futurs types) avec :
- **Partie commune** : Propriétés `ProjectBase` (Name, Status, DisplayImage, Transformations) → identique pour tous
- **Partie spécifique** : Propriétés concrètes (TileShape, SourceImage, WallHeight, etc.) → **différentes par type**

### Contraintes DDD
- ❌ Les entités Core **ne doivent PAS connaître l'UI** (pas d'attributs `[Display]`, pas de `INotifyPropertyChanged`)
- ❌ Les entités Core **ne sont PAS observables** (violation de responsabilité métier)
- ✅ Séparation stricte : Presentation ↔ Core (dépendance unidirectionnelle)

---

## Décision d'architecture : DataTemplateSelector + ViewModels typés

### Approche choisie ⭐

**Pattern MAUI** : **DataTemplateSelector with Typed ViewModels**

**Principe** :
1. Une **Page principale** (`EditProjectPage`) contient les sections communes
2. Un **ContentView** avec `DataTemplateSelector` choisit dynamiquement la vue spécifique selon le type concret
3. Chaque type de projet a son **triplet** : `View.xaml` + `View.xaml.cs` + `ViewModel.cs`

---

## Structure de fichiers

```
TileTextureGenerator.Presentation.UI/
├─ Pages/
│  ├─ ManageProjectListPage.xaml             # Liste des projets
│  └─ EditProjectPage.xaml                   # Édition (partie commune)
│
├─ Views/
│  └─ ProjectDetails/                        # Vues spécifiques par type
│     ├─ FloorTileProjectDetailsView.xaml
│     ├─ FloorTileProjectDetailsView.xaml.cs
│     ├─ FloorTileProjectDetailsViewModel.cs
│     ├─ WallTileProjectDetailsView.xaml
│     ├─ WallTileProjectDetailsView.xaml.cs
│     └─ WallTileProjectDetailsViewModel.cs
│
├─ ViewModels/
│  ├─ ManageProjectListViewModel.cs          # Liste
│  └─ EditProjectViewModel.cs                # Coordination générale
│
└─ Selectors/
   └─ ProjectTypeTemplateSelector.cs         # Switch types → templates
```

---

## Architecture détaillée

### 1. EditProjectPage (Page principale)

**Responsabilité** : Coordonner l'affichage des sections communes et spécifiques.

**Sections** :
```
┌────────────────────────────────────────────┐
│  EditProjectPage.xaml                      │
├────────────────────────────────────────────┤
│  1. Section ProjectBase (statique)         │
│     - Name (ReadOnly)                      │
│     - Status                               │
│     - DisplayImage                         │
│     - LastModifiedDate                     │
├────────────────────────────────────────────┤
│  2. ContentView dynamique                  │
│     └─ ProjectTypeTemplateSelector         │
│        ├─ FloorTileDetailsView (si Floor)  │
│        └─ WallTileDetailsView (si Wall)    │
├────────────────────────────────────────────┤
│  3. Section Transformations (statique)     │
│     - CollectionView (liste)               │
│     - Add/Remove/Edit commands             │
├────────────────────────────────────────────┤
│  4. Actions (statique)                     │
│     - Generate PDF                         │
│     - Archive                              │
│     - Save changes                         │
└────────────────────────────────────────────┘
```

**XAML structure** :
```xaml
<ContentPage xmlns="..." x:Class="...EditProjectPage">
    <!-- Section 1: Common properties (ProjectBase) -->
    <StackLayout>
        <Entry Text="{Binding Project.Name}" IsReadOnly="True" />
        <Label Text="{Binding Project.Status}" />
        <Image Source="{Binding Project.DisplayImage}" />
    </StackLayout>
    
    <!-- Section 2: Type-specific properties (ContentView + Selector) -->
    <ContentView Content="{Binding Project}">
        <ContentView.ContentTemplate>
            <local:ProjectTypeTemplateSelector>
                <local:ProjectTypeTemplateSelector.FloorTileTemplate>
                    <DataTemplate>
                        <views:FloorTileProjectDetailsView />
                    </DataTemplate>
                </local:ProjectTypeTemplateSelector.FloorTileTemplate>
                
                <local:ProjectTypeTemplateSelector.WallTileTemplate>
                    <DataTemplate>
                        <views:WallTileProjectDetailsView />
                    </DataTemplate>
                </local:ProjectTypeTemplateSelector.WallTileTemplate>
            </local:ProjectTypeTemplateSelector>
        </ContentView.ContentTemplate>
    </ContentView>
    
    <!-- Section 3: Transformations -->
    <CollectionView ItemsSource="{Binding Project.Transformations}">
        <!-- ... -->
    </CollectionView>
    
    <!-- Section 4: Actions -->
    <Button Text="Validate Changes" Command="{Binding SaveCommand}" />
</ContentPage>
```

---

### 2. ProjectTypeTemplateSelector

**Responsabilité** : Sélectionner le bon DataTemplate selon le type concret du projet.

**Code** :
```csharp
namespace TileTextureGenerator.Presentation.UI.Selectors;

/// <summary>
/// Selects the appropriate DataTemplate based on the concrete project type.
/// Each registered project type must have a corresponding template property.
/// </summary>
public class ProjectTypeTemplateSelector : DataTemplateSelector
{
    public DataTemplate FloorTileTemplate { get; set; } = null!;
    public DataTemplate WallTileTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            FloorTileProject => FloorTileTemplate 
                ?? throw new InvalidOperationException("FloorTileTemplate not initialized."),
            
            WallTileProject => WallTileTemplate 
                ?? throw new InvalidOperationException("WallTileTemplate not initialized."),
            
            _ => throw new NotSupportedException(
                $"No DataTemplate defined for project type: {item?.GetType().Name}. " +
                $"Add a new template property and case in OnSelectTemplate.")
        };
    }
}
```

**Contrat** : Pour chaque type dans `TextureProjectRegistry`, il DOIT exister :
1. Une propriété `DataTemplate` dans le selector
2. Un case dans le `switch` expression

---

### 3. FloorTileProjectDetailsView + ViewModel

**Responsabilité** : Éditer les propriétés spécifiques à FloorTileProject.

#### FloorTileProjectDetailsView.xaml
```xaml
<ContentView xmlns="..." 
             x:Class="...FloorTileProjectDetailsView"
             x:DataType="vm:FloorTileProjectDetailsViewModel">
    
    <StackLayout>
        <Label Text="Tile Shape" />
        <Picker ItemsSource="{Binding AvailableTileShapes}" 
                SelectedItem="{Binding SelectedTileShape}" />
        
        <Label Text="Source Image" />
        <local:ImagePickerControl ImageData="{Binding SelectedSourceImage}" />
    </StackLayout>
</ContentView>
```

#### FloorTileProjectDetailsView.xaml.cs
```csharp
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

### 2️⃣ Presentation.UI (Views)
- [ ] Créer `Views/ProjectDetails/PillarTileProjectDetailsView.xaml`
- [ ] Créer `Views/ProjectDetails/PillarTileProjectDetailsView.xaml.cs`
- [ ] Créer `Views/ProjectDetails/PillarTileProjectDetailsViewModel.cs`

### 3️⃣ Selector
- [ ] Ajouter propriété dans `ProjectTypeTemplateSelector.cs` :
  ```csharp
  public DataTemplate PillarTileTemplate { get; set; } = null!;
  ```
- [ ] Ajouter case dans `OnSelectTemplate` :
  ```csharp
  PillarTileProject => PillarTileTemplate ?? throw ...,
  ```

### 4️⃣ Page principale
- [ ] Référencer template dans `EditProjectPage.xaml` :
  ```xaml
  <local:ProjectTypeTemplateSelector.PillarTileTemplate>
      <DataTemplate><views:PillarTileProjectDetailsView /></DataTemplate>
  </local:ProjectTypeTemplateSelector.PillarTileTemplate>
  ```

### 5️⃣ Ressources (Localisation)
- [ ] Ajouter clés dans fichier `.resx` :
  - `ProjectProperty_PillarTileProject_Height`
  - `ProjectProperty_PillarTileProject_Width`
  - etc.

### 6️⃣ Tests
- [ ] **Exécuter** `dotnet test` pour valider l'intégrité
- [ ] Le test `AllRegisteredProjectTypes_HaveCorrespondingDataTemplate` **doit passer** ✅

**Si le test échoue** → Vérifier les étapes 2-4.

---

## Tests d'intégrité

### Test Registry ↔ TemplateSelector

**Fichier** : `Presentation.UI.Tests/Selectors/ProjectTypeTemplateSelectorTests.cs`

**Objectif** : Garantir que tous les types enregistrés dans `TextureProjectRegistry` ont un DataTemplate correspondant.

**Stratégie** :
1. Lire `TextureProjectRegistry.GetAllRegisteredTypes()`
2. Pour chaque type, créer instance temporaire via factory
3. Appeler `selector.OnSelectTemplate(instance, null)`
4. Vérifier retour non-null (pas d'exception)

**Détection** :
- ✅ Lors de `dotnet test` (avant commit)
- ✅ Dans CI/CD (si configuré)

**Message d'erreur** :
```
System.NotSupportedException: No DataTemplate defined for project type: PillarTileProject.
Add a new template property and case in OnSelectTemplate.
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
