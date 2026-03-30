using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TileTextureGenerator.Adapters.UseCases;
using TileTextureGenerator.Presentation.UI.Services;

namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// ViewModel for the project list management page.
/// Handles user input for project name and type selection,
/// and coordinates with ManageProjectListUseCase for project management.
/// </summary>
public class ManageProjectListViewModel : INotifyPropertyChanged
{
    private readonly ManageProjectListUseCase _manageProjectListUseCase;
    private readonly ProjectTypeLocalizer _projectTypeLocalizer;
    
    private string _projectName = string.Empty;
    private string? _selectedProjectType;
    private ProjectTypeItem? _selectedProjectTypeItem;
    private bool _isBusy;
    private string? _errorMessage;
    private bool _projectAlreadyExists;
    // Commands for project card actions
    public ICommand LoadProjectCommand { get; }
    public ICommand GeneratePdfCommand { get; }
    public ICommand ArchiveProjectCommand { get; }
    public ICommand DeleteProjectCommand { get; }

    public ManageProjectListViewModel(
        ManageProjectListUseCase manageProjectListUseCase,
        ProjectTypeLocalizer projectTypeLocalizer)
    {
        LoadProjectCommand = new Command<ProjectListItemDto>(async (project) => await OnLoadProjectAsync(project));
        GeneratePdfCommand = new Command<ProjectListItemDto>(async (project) => await OnGeneratePdfAsync(project));
        ArchiveProjectCommand = new Command<ProjectListItemDto>(async (project) => await OnArchiveProjectAsync(project));
        DeleteProjectCommand = new Command<ProjectListItemDto>(async (project) => await OnDeleteProjectAsync(project));
        ArgumentNullException.ThrowIfNull(manageProjectListUseCase);
        ArgumentNullException.ThrowIfNull(projectTypeLocalizer);

        _manageProjectListUseCase = manageProjectListUseCase;
        _projectTypeLocalizer = projectTypeLocalizer;

        ProjectTypes = [];
        CreateProjectCommand = new Command(async () => await CreateProjectAsync(), CanCreateProject);
        CycleLanguageCommand = new Command(CycleLanguage);

        // Subscribe to language changes to refresh project types
        _projectTypeLocalizer.LanguageChanged += OnLanguageChanged;
        
        // Load project types on initialization
        _ = LoadProjectTypesAsync();
        // Load projects on initialization
        _ = LoadProjectsAsync();
    }

    // Handlers for project card actions
    private async Task OnLoadProjectAsync(ProjectListItemDto project)
    {
        // Call appropriate use case (load project)
        await LoadProjectsAsync();
    }

    private async Task OnGeneratePdfAsync(ProjectListItemDto project)
    {
        // Call appropriate use case (generate PDF)
        await LoadProjectsAsync();
    }

    private async Task OnArchiveProjectAsync(ProjectListItemDto project)
    {
        // Call appropriate use case (archive project)
        await LoadProjectsAsync();
    }

    private async Task OnDeleteProjectAsync(ProjectListItemDto project)
    {
        if (project is null || string.IsNullOrWhiteSpace(project.Name))
            return;

        try
        {
            // Get localized confirmation strings
            var title = _projectTypeLocalizer.GetLocalizedString("DeleteProject_ConfirmTitle");
            var message = string.Format(
                _projectTypeLocalizer.GetLocalizedString("DeleteProject_ConfirmMessage"),
                project.Name);
            var confirmText = _projectTypeLocalizer.GetLocalizedString("DeleteProject_ConfirmYes");
            var cancelText = _projectTypeLocalizer.GetLocalizedString("DeleteProject_ConfirmNo");

            // Show confirmation dialog
            if (Application.Current?.MainPage is null)
                return;

            var userConfirmed = await Application.Current.MainPage.DisplayAlert(
                title,
                message,
                confirmText,
                cancelText);

            if (!userConfirmed)
                return; // User cancelled

            // Proceed with deletion
            IsBusy = true;
            ErrorMessage = null;

            var result = await _manageProjectListUseCase.DeleteProjectAsync(project.Name);

            if (result.IsSuccess)
            {
                // Success: Refresh project list
                await LoadProjectsAsync();
            }
            else
            {
                // Display error message
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete project: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    /// <summary>
    /// Collection of projects for display in the UI.
    /// </summary>
    public ObservableCollection<ProjectListItemDto> Projects { get; } = new();

    /// <summary>
    /// Loads the list of projects for display.
    /// </summary>
    public async Task LoadProjectsAsync()
    {
        try
        {
            IsBusy = true;
            Projects.Clear();
            var result = await _manageProjectListUseCase.ListProjectsAsync();
            if (result.IsSuccess && result.Projects is not null)
            {
                foreach (var project in result.Projects)
                    Projects.Add(project);
            }
            else if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load projects: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
                OnPropertyChanged(nameof(IsCreateButtonActive));
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
                OnPropertyChanged(nameof(IsCreateButtonActive));
            }
        }
    }

    /// <summary>
    /// Indicates whether the Create button should appear active (green).
    /// True when all conditions are met: name not empty, name doesn't exist, type selected.
    /// </summary>
    public bool IsCreateButtonActive => CanCreateProject();

    /// <summary>
    /// Gets the current language flag emoji for the toolbar button.
    /// </summary>
    public string CurrentLanguageFlag => _projectTypeLocalizer.CurrentLanguage.FlagImageSource;

    #endregion

    #region Commands

    /// <summary>
    /// Command to create a new project.
    /// </summary>
    public ICommand CreateProjectCommand { get; }
    /// <summary>
    /// Command to cycle through supported languages.
    /// </summary>
    public ICommand CycleLanguageCommand { get; }

    #endregion

    #region Methods

    private async Task LoadProjectTypesAsync()
    {
        try
        {
            IsBusy = true;
            var technicalTypes = await _manageProjectListUseCase.LoadProjectTypesAsync();

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
            ProjectAlreadyExists = await _manageProjectListUseCase.ProjectExistsAsync(ProjectName);

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

            var result = await _manageProjectListUseCase.CreateProjectAsync(ProjectName, SelectedProjectType!);

            if (result.IsSuccess)
            {
                // Success: Clear form
                ProjectName = string.Empty;
                SelectedProjectTypeItem = null; // This will also set SelectedProjectType to null
                ErrorMessage = null;

                // Refresh project list after creation
                await LoadProjectsAsync();
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

    private void CycleLanguage()
    {
        _projectTypeLocalizer.CycleLanguage();
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        // Update language flag in toolbar
        OnPropertyChanged(nameof(CurrentLanguageFlag));

        // Reload project types with new language
        await LoadProjectTypesAsync();

        // Show simple alert notification in the NEW language
        var message = _projectTypeLocalizer.CurrentLanguage.Code == "fr" 
            ? "Langue modifiée. Redémarrez l'application pour appliquer les changements."
            : "Language changed. Restart the application to apply changes.";

        // For now, use DisplayAlert as a fallback (can be improved later)
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(
                _projectTypeLocalizer.CurrentLanguage.Code == "fr" ? "Information" : "Information", 
                message, 
                _projectTypeLocalizer.CurrentLanguage.Code == "fr" ? "OK" : "OK");
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
