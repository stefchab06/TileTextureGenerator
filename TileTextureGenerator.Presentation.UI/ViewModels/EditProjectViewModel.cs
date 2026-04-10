using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TileTextureGenerator.Adapters.UseCases;
using TileTextureGenerator.Presentation.UI.Services;

namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// ViewModel for editing a project.
/// Coordinates UI operations with EditProjectUseCase.
/// </summary>
public class EditProjectViewModel : INotifyPropertyChanged
{
    private readonly EditProjectUseCase _editUseCase;
    private readonly ProjectTypeLocalizer _projectTypeLocalizer;
    private readonly TransformationTypeLocalizer _transformationTypeLocalizer;
    private readonly TileShapeLocalizer _tileShapeLocalizer;
    private readonly System.Text.Json.Nodes.JsonObject _propertiesJson;
    private TransformationTypeItem? _selectedTransformationType;
    private bool _isSaving;
    private bool _isTransformationPickerExpanded;
    private object? _projectViewModel;

    public EditProjectViewModel(
        EditProjectUseCase editUseCase, 
        ProjectTypeLocalizer projectTypeLocalizer, 
        TransformationTypeLocalizer transformationTypeLocalizer,
        TileShapeLocalizer tileShapeLocalizer)
    {
        ArgumentNullException.ThrowIfNull(editUseCase);
        ArgumentNullException.ThrowIfNull(projectTypeLocalizer);
        ArgumentNullException.ThrowIfNull(transformationTypeLocalizer);
        ArgumentNullException.ThrowIfNull(tileShapeLocalizer);

        _editUseCase = editUseCase;
        _projectTypeLocalizer = projectTypeLocalizer;
        _transformationTypeLocalizer = transformationTypeLocalizer;
        _tileShapeLocalizer = tileShapeLocalizer;

        // Get JSON once and share it with template ViewModels
        _propertiesJson = _editUseCase.GetPropertiesJson();

        SaveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
        AddTransformationCommand = new Command(async () => await AddTransformationAsync(), CanAddTransformation);
        ToggleTransformationPickerCommand = new Command(ToggleTransformationPicker);
        SelectTransformationTypeCommand = new Command<TransformationTypeItem>(OnTransformationTypeSelected);
        RemoveTransformationCommand = new Command<Guid>(async (id) => await RemoveTransformationAsync(id));

        // Create ViewModel wrapper for the project (shares _propertiesJson reference)
        _projectViewModel = CreateProjectViewModel();

        // Load available transformation types
        _ = LoadTransformationTypesAsync();

        // Load existing transformations
        LoadTransformations();
    }

    /// <summary>
    /// Concrete type name of the project (for template selection).
    /// </summary>
    public string ConcreteTypeName => _editUseCase.ConcreteTypeName;

    /// <summary>
    /// ViewModel wrapper for the project (for DataTemplateSelector).
    /// </summary>
    public object? ProjectViewModel => _projectViewModel;

    /// <summary>
    /// Tile shape localizer (for template ViewModels).
    /// </summary>
    public TileShapeLocalizer TileShapeLocalizer => _tileShapeLocalizer;

    /// <summary>
    /// Project name for Shell title.
    /// </summary>
    public string ProjectName => _editUseCase.Project.Name;

    /// <summary>
    /// Localized project type for Shell title.
    /// </summary>
    public string ProjectTypeLocalized => _projectTypeLocalizer.GetLocalizedName(_editUseCase.Project.Type);

    /// <summary>
    /// Shell title: "ProjectName (Type)".
    /// </summary>
    public string ShellTitle => $"{ProjectName} ({ProjectTypeLocalized})";

    /// <summary>
    /// Available transformation types for picker (image + technical name).
    /// </summary>
    public ObservableCollection<TransformationTypeItem> AvailableTransformationTypes { get; } = [];

    /// <summary>
    /// List of transformations configured for this project (for card display).
    /// </summary>
    public ObservableCollection<TransformationCardItem> Transformations { get; } = [];

    /// <summary>
    /// Selected transformation type in picker.
    /// </summary>
    public TransformationTypeItem? SelectedTransformationType
    {
        get => _selectedTransformationType;
        set
        {
            if (SetProperty(ref _selectedTransformationType, value))
            {
                ((Command)AddTransformationCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(IsAddButtonActive));
            }
        }
    }

    /// <summary>
    /// Indicates if a save operation is in progress.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                ((Command)SaveCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(IsAddButtonActive));
            }
        }
    }

    /// <summary>
    /// Indicates if the transformation picker is expanded.
    /// </summary>
    public bool IsTransformationPickerExpanded
    {
        get => _isTransformationPickerExpanded;
        set => SetProperty(ref _isTransformationPickerExpanded, value);
    }

    /// <summary>
    /// Indicates if the Add Transformation button is active (green) or inactive (gray).
    /// </summary>
    public bool IsAddButtonActive => SelectedTransformationType != null && !IsSaving;

    public ICommand SaveCommand { get; }
    public ICommand AddTransformationCommand { get; }
    public ICommand ToggleTransformationPickerCommand { get; }
    public ICommand SelectTransformationTypeCommand { get; }
    public ICommand RemoveTransformationCommand { get; }

    private async Task LoadTransformationTypesAsync()
    {
        try
        {
            var types = await _editUseCase.GetAvailableTransformationTypesAsync();

            foreach (var (technicalName, iconBytes) in types)
            {
                AvailableTransformationTypes.Add(new TransformationTypeItem
                {
                    TechnicalName = technicalName,
                    LocalizedName = _transformationTypeLocalizer.GetLocalizedName(technicalName),
                    Icon = iconBytes
                });
            }
        }
        catch (Exception ex)
        {
            // TODO: Error handling (future iteration)
            System.Diagnostics.Debug.WriteLine($"Failed to load transformation types: {ex.Message}");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            // Update entity from JSON (shared reference, already modified by template)
            _editUseCase.UpdatePropertiesFromJson(_propertiesJson);

            await _editUseCase.SaveAsync();

            // TODO: Show success message (future iteration)
        }
        catch (Exception ex)
        {
            // TODO: Error handling (future iteration)
            System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanAddTransformation()
    {
        return SelectedTransformationType != null && !IsSaving;
    }

    private async Task AddTransformationAsync()
    {
        if (SelectedTransformationType == null)
            return;

        try
        {
            await _editUseCase.AddTransformationAsync(SelectedTransformationType.TechnicalName);

            // Reset selection and collapse picker after adding
            SelectedTransformationType = null;
            IsTransformationPickerExpanded = false;

            // Refresh transformations list
            LoadTransformations();
        }
        catch (Exception ex)
        {
            // TODO: Error handling (future iteration)
            System.Diagnostics.Debug.WriteLine($"Add transformation failed: {ex.Message}");
        }
    }

    private async Task RemoveTransformationAsync(Guid transformationId)
    {
        if (transformationId == Guid.Empty)
            return;

        try
        {
            await _editUseCase.RemoveTransformationAsync(transformationId);

            // Refresh transformations list
            LoadTransformations();
        }
        catch (Exception ex)
        {
            // TODO: Error handling (future iteration)
            System.Diagnostics.Debug.WriteLine($"Remove transformation failed: {ex.Message}");
        }
    }

    private void LoadTransformations()
    {
        Transformations.Clear();

        var transformations = _editUseCase.GetTransformations();
        foreach (var transformation in transformations)
        {
            // Convert byte[] to ImageSource
            ImageSource? iconSource = null;
            if (transformation.Icon != null && transformation.Icon.Length > 0)
            {
                iconSource = ImageSource.FromStream(() => new System.IO.MemoryStream(transformation.Icon));
            }

            Transformations.Add(new TransformationCardItem
            {
                Id = transformation.Id,
                TechnicalName = transformation.TypeName,
                LocalizedName = _transformationTypeLocalizer.GetLocalizedName(transformation.TypeName),
                IconSource = iconSource
            });
        }
    }

    private void ToggleTransformationPicker()
    {
        IsTransformationPickerExpanded = !IsTransformationPickerExpanded;
    }

    private void OnTransformationTypeSelected(TransformationTypeItem item)
    {
        SelectedTransformationType = item;
        IsTransformationPickerExpanded = false;
        ((Command)AddTransformationCommand).ChangeCanExecute();
    }

    /// <summary>
    /// Creates the appropriate ViewModel wrapper based on project type.
    /// Uses reflection and naming convention to instantiate the correct ViewModel.
    /// Convention: "FloorTileProject" → "FloorTileProjectViewModel(JsonObject, EditProjectViewModel)"
    /// </summary>
    private object? CreateProjectViewModel()
    {
        // Get ViewModel type by convention (e.g., "FloorTileProject" → "FloorTileProjectViewModel")
        var viewModelTypeName = $"TileTextureGenerator.Presentation.UI.ViewModels.{_editUseCase.ConcreteTypeName}ViewModel";
        var viewModelType = Type.GetType(viewModelTypeName);

        if (viewModelType == null)
            return null; // No ViewModel found → PlaceholderTemplate will be used

        try
        {
            // Instantiate with unified signature: (JsonObject, EditProjectViewModel)
            var instance = Activator.CreateInstance(viewModelType, _propertiesJson, this);
            return instance;
        }
        catch
        {
            // Failed to instantiate → PlaceholderTemplate will be used
            return null;
        }
    }

    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Item for transformation type picker (icon + technical name + localized name).
/// </summary>
public class TransformationTypeItem
{
    public string TechnicalName { get; set; } = string.Empty;
    public string LocalizedName { get; set; } = string.Empty;
    public byte[] Icon { get; set; } = Array.Empty<byte>();
}
