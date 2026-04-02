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
    private TransformationTypeItem? _selectedTransformationType;
    private bool _isSaving;

    public EditProjectViewModel(EditProjectUseCase editUseCase, ProjectTypeLocalizer projectTypeLocalizer)
    {
        ArgumentNullException.ThrowIfNull(editUseCase);
        ArgumentNullException.ThrowIfNull(projectTypeLocalizer);

        _editUseCase = editUseCase;
        _projectTypeLocalizer = projectTypeLocalizer;

        SaveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
        AddTransformationCommand = new Command(async () => await AddTransformationAsync(), CanAddTransformation);

        // Load available transformation types
        _ = LoadTransformationTypesAsync();
    }

    /// <summary>
    /// Exposes the project for binding (Shell title, future template selector).
    /// </summary>
    public TileTextureGenerator.Core.Entities.ProjectBase Project => _editUseCase.Project;

    /// <summary>
    /// Project name for Shell title.
    /// </summary>
    public string ProjectName => Project.Name;

    /// <summary>
    /// Localized project type for Shell title.
    /// </summary>
    public string ProjectTypeLocalized => _projectTypeLocalizer.GetLocalizedName(Project.Type);

    /// <summary>
    /// Shell title: "ProjectName - Type".
    /// </summary>
    public string ShellTitle => $"{ProjectName} - {ProjectTypeLocalized}";

    /// <summary>
    /// Available transformation types for picker (image + technical name).
    /// </summary>
    public ObservableCollection<TransformationTypeItem> AvailableTransformationTypes { get; } = [];

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
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand AddTransformationCommand { get; }

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
            
            // Reset selection after adding
            SelectedTransformationType = null;
            
            // TODO: Refresh transformations list (future iteration)
        }
        catch (Exception ex)
        {
            // TODO: Error handling (future iteration)
            System.Diagnostics.Debug.WriteLine($"Add transformation failed: {ex.Message}");
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
/// Item for transformation type picker (icon + technical name).
/// </summary>
public class TransformationTypeItem
{
    public string TechnicalName { get; set; } = string.Empty;
    public byte[] Icon { get; set; } = Array.Empty<byte>();
}
