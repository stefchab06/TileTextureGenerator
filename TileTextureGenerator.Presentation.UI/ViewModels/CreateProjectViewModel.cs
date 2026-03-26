using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TileTextureGenerator.Adapters.UseCases;
using TileTextureGenerator.Presentation.UI.Services;

namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// ViewModel for the project creation page.
/// Handles user input for project name and type selection,
/// and coordinates with CreateProjectUseCase for project creation.
/// </summary>
public class CreateProjectViewModel : INotifyPropertyChanged
{
    private readonly CreateProjectUseCase _createProjectUseCase;
    private readonly ProjectTypeLocalizer _projectTypeLocalizer;
    
    private string _projectName = string.Empty;
    private string? _selectedProjectType;
    private ProjectTypeItem? _selectedProjectTypeItem;
    private bool _isBusy;
    private string? _errorMessage;
    private bool _projectAlreadyExists;

    public CreateProjectViewModel(
        CreateProjectUseCase createProjectUseCase,
        ProjectTypeLocalizer projectTypeLocalizer)
    {
        ArgumentNullException.ThrowIfNull(createProjectUseCase);
        ArgumentNullException.ThrowIfNull(projectTypeLocalizer);

        _createProjectUseCase = createProjectUseCase;
        _projectTypeLocalizer = projectTypeLocalizer;

        ProjectTypes = [];
        CreateProjectCommand = new Command(async () => await CreateProjectAsync(), CanCreateProject);
        
        // Load project types on initialization
        _ = LoadProjectTypesAsync();
    }

    #region Properties

    /// <summary>
    /// Name entered by the user for the new project.
    /// </summary>
    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (SetProperty(ref _projectName, value))
            {
                ErrorMessage = null; // Clear error when user types
                _ = CheckProjectExistsAsync(); // Check if project already exists (async, fire-and-forget)
                ((Command)CreateProjectCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(IsCreateButtonActive)); // Update button color
            }
        }
    }

    /// <summary>
    /// Technical name of the selected project type (e.g., "FloorTileProject").
    /// </summary>
    public string? SelectedProjectType
    {
        get => _selectedProjectType;
        set
        {
            if (SetProperty(ref _selectedProjectType, value))
            {
                ErrorMessage = null;
                ((Command)CreateProjectCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(IsCreateButtonActive)); // Update button color
            }
        }
    }

    /// <summary>
    /// Selected project type item (for Picker binding).
    /// Synchronizes with SelectedProjectType.
    /// </summary>
    public ProjectTypeItem? SelectedProjectTypeItem
    {
        get => _selectedProjectTypeItem;
        set
        {
            if (SetProperty(ref _selectedProjectTypeItem, value))
            {
                // Update the technical name when item changes
                SelectedProjectType = value?.TechnicalName;
            }
        }
    }

    /// <summary>
    /// Collection of available project types with localized names.
    /// Key = technical name, Value = localized display name.
    /// </summary>
    public ObservableCollection<ProjectTypeItem> ProjectTypes { get; }

    /// <summary>
    /// Indicates whether an async operation is in progress.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Error message to display to the user, if any.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Indicates whether a project with the current name already exists.
    /// Used to disable the Create button when true.
    /// </summary>
    public bool ProjectAlreadyExists
    {
        get => _projectAlreadyExists;
        private set
        {
            if (SetProperty(ref _projectAlreadyExists, value))
            {
                ((Command)CreateProjectCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(IsCreateButtonActive)); // Notify UI for color change
            }
        }
    }

    /// <summary>
    /// Indicates whether the Create button should appear active (green).
    /// True when all conditions are met: name not empty, name doesn't exist, type selected.
    /// </summary>
    public bool IsCreateButtonActive => CanCreateProject();

    #endregion

    #region Commands

    /// <summary>
    /// Command to create a new project.
    /// </summary>
    public ICommand CreateProjectCommand { get; }

    #endregion

    #region Methods

    private async Task LoadProjectTypesAsync()
    {
        try
        {
            IsBusy = true;
            var technicalTypes = await _createProjectUseCase.LoadProjectTypesAsync();
            
            ProjectTypes.Clear();
            foreach (var technicalType in technicalTypes)
            {
                var localizedName = _projectTypeLocalizer.GetLocalizedName(technicalType);
                ProjectTypes.Add(new ProjectTypeItem(technicalType, localizedName));
            }

            // Do NOT select a type by default - force user to select explicitly
            SelectedProjectType = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load project types: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreateProject()
    {
        return !IsBusy 
               && !string.IsNullOrWhiteSpace(ProjectName) 
               && !string.IsNullOrWhiteSpace(SelectedProjectType)
               && !ProjectAlreadyExists; // Disable if project already exists
    }

    private async Task CheckProjectExistsAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            ProjectAlreadyExists = false;
            return;
        }

        try
        {
            ProjectAlreadyExists = await _createProjectUseCase.ProjectExistsAsync(ProjectName);

            if (ProjectAlreadyExists)
            {
                ErrorMessage = $"A project with the name '{ProjectName}' already exists.";
            }
            else
            {
                ErrorMessage = null;
            }
        }
        catch (Exception)
        {
            // Don't block user input on check failure
            ProjectAlreadyExists = false;
        }
    }

    private async Task CreateProjectAsync()
    {
        if (!CanCreateProject())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _createProjectUseCase.ExecuteAsync(ProjectName, SelectedProjectType!);

            if (result.IsSuccess)
            {
                // Success: Clear form
                ProjectName = string.Empty;
                SelectedProjectTypeItem = null; // This will also set SelectedProjectType to null
                ErrorMessage = null;

                // TODO: Navigate to project details or show success message
                // For now, just log success
            }
            else
            {
                // Display error message
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            ((Command)CreateProjectCommand).ChangeCanExecute();
        }
    }

    #endregion

    #region INotifyPropertyChanged

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

    #endregion
}

/// <summary>
/// Represents a project type with both technical and localized names.
/// </summary>
public record ProjectTypeItem(string TechnicalName, string LocalizedName);
