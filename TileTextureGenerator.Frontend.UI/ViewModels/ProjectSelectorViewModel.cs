using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Frontend.UI.Models;
using TileTextureGenerator.Frontend.UI.Services;

namespace TileTextureGenerator.Frontend.UI.ViewModels;

public partial class ProjectSelectorViewModel : ObservableObject
{
    private readonly IProjectSelectionUseCase _projectSelectionUseCase;

    [ObservableProperty]
    private string projectName = string.Empty;

    [ObservableProperty]
    private LocalizedValue<string>? projectType;

    [ObservableProperty]
    private ObservableCollection<LocalizedValue<string>> projectTypes = [];

    [ObservableProperty]
    private ObservableCollection<ProjectDisplayItem> projects = [];

    [ObservableProperty]
    private ProjectDisplayItem? selectedProject;

    [ObservableProperty]
    private string createOpenButtonText = string.Empty;

    [ObservableProperty]
    private bool isProjectTypeEnabled = true;

    public ProjectSelectorViewModel(IProjectSelectionUseCase projectSelectionUseCase)
    {
        _projectSelectionUseCase = projectSelectionUseCase;

        // Initialize button text
        createOpenButtonText = LocalizationService.Instance.GetString("ProjectSelector_Create");
    }
    public async Task InitializeAsync()
    {
        // Clear existing data
        ProjectTypes.Clear();
        Projects.Clear();

        // Load project types
        var projectTypesList = await _projectSelectionUseCase.GetProjectTypeListAsync();

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"InitializeAsync: Found {projectTypesList.Count} project types");
#endif

        foreach (var pt in projectTypesList)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  - Project type: {pt}");
#endif
            var localized = LocalizationService.Instance.GetString("ProjectType_" + pt);
            ProjectTypes.Add(new LocalizedValue<string>(pt, localized));
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"InitializeAsync: ProjectTypes collection now has {ProjectTypes.Count} items");
#endif

        // Load existing projects (using optimized summaries)
        var projectsList = await _projectSelectionUseCase.GetProjectSummariesAsync();

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"InitializeAsync: Found {projectsList.Count} existing projects");
#endif

        foreach (var p in projectsList)
        {
            var localType = LocalizationService.Instance.GetString("ProjectType_" + p.Type);
            var localStatus = LocalizationService.Instance.GetString("ProjectStatus_" + p.Status);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  - Project: {p.Name} | Type: {p.Type} ({localType}) | Status: {p.Status} ({localStatus})");
#endif

            Projects.Add(new ProjectDisplayItem(p.Name, p.Type, localType, p.Status, localStatus, p.DisplayImage));
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"InitializeAsync: Projects collection now has {Projects.Count} items");
#endif
    }

    partial void OnProjectNameChanged(string value)
    {
        UpdateCreateOpenButton();
    }

    partial void OnSelectedProjectChanged(ProjectDisplayItem? value)
    {
        if (value is null) return;

        LoadProjectIntoForm(value);
    }

    [RelayCommand]
    private void SelectProject(ProjectDisplayItem project)
    {
        if (project is null) return;

        LoadProjectIntoForm(project);

        // Reset the selection to allow re-selecting the same project
        SelectedProject = null;
    }

    private void LoadProjectIntoForm(ProjectDisplayItem project)
    {
        ProjectName = project.Name;
        // Find the matching LocalizedValue for the type
        ProjectType = ProjectTypes.FirstOrDefault(pt => pt.Value == project.Type);

        UpdateCreateOpenButton();
    }

    [RelayCommand]
    private async Task CreateOrOpenProjectAsync()
    {
        var name = ProjectName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await Shell.Current.DisplayAlertAsync(
                LocalizationService.Instance.GetString("Error_Title"),
                LocalizationService.Instance.GetString("Error_ProjectNameRequired"),
                "OK");
            return;
        }

        var exists = Projects.Any(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            // Open existing project
            await OpenProjectAsync(name);
        }
        else
        {
            // Create new project
            await CreateProjectAsync(name);
        }
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(ProjectDisplayItem project)
    {
        if (project is null) return;

        // Confirmation dialog
        var confirmTitle = LocalizationService.Instance.GetString("ProjectManagement_DeleteConfirmTitle");
        var confirmMessage = string.Format(
            LocalizationService.Instance.GetString("ProjectManagement_DeleteConfirmMessage"),
            project.Name);
        var yes = LocalizationService.Instance.GetString("Common_Yes");
        var no = LocalizationService.Instance.GetString("Common_No");

        bool confirmed = await Shell.Current.DisplayAlertAsync(confirmTitle, confirmMessage, yes, no);

        if (!confirmed) return;

        var dto = new Adapters.UseCases.Dto.TextureProjectDto(project.Name, project.Type, project.Status);
        await _projectSelectionUseCase.DeleteAsync(dto);
        await InitializeAsync();
    }

    private async Task OpenProjectAsync(string name)
    {
        var project = Projects.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            await Shell.Current.DisplayAlertAsync(
                LocalizationService.Instance.GetString("Error_Title"),
                LocalizationService.Instance.GetString("Error_ProjectNotFound"),
                "OK");
            return;
        }

        var dto = new Adapters.UseCases.Dto.TextureProjectDto(project.Name, project.Type, project.Status);
        var opened = await _projectSelectionUseCase.OpenAsync(dto);

        if (opened is null)
        {
            await Shell.Current.DisplayAlertAsync(
                LocalizationService.Instance.GetString("Error_Title"),
                LocalizationService.Instance.GetString("Error_ProjectLoadFailed"),
                "OK");
            return;
        }

        // Refresh list and clear form
        await InitializeAsync();
        ProjectName = string.Empty;
        ProjectType = null;
        UpdateCreateOpenButton();
    }

    private async Task CreateProjectAsync(string name)
    {
        if (ProjectType is null)
        {
            await Shell.Current.DisplayAlertAsync(
                LocalizationService.Instance.GetString("Error_Title"),
                LocalizationService.Instance.GetString("Error_ProjectTypeRequired"),
                "OK");
            return;
        }

        var dto = new Adapters.UseCases.Dto.TextureProjectDto(name, ProjectType.Value, "Unexisting");
        var created = await _projectSelectionUseCase.CreateAsync(dto);

        if (created is null)
        {
            await Shell.Current.DisplayAlertAsync(
                LocalizationService.Instance.GetString("Error_Title"),
                LocalizationService.Instance.GetString("Error_ProjectCreationFailed"),
                "OK");
            return;
        }

        // Refresh list and clear form
        await InitializeAsync();
        ProjectName = string.Empty;
        ProjectType = null;
        UpdateCreateOpenButton();
    }

    private void UpdateCreateOpenButton()
    {
        var name = ProjectName?.Trim() ?? string.Empty;
        var exists = !string.IsNullOrEmpty(name) &&
                     Projects.Any(p =>
                         string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        var key = exists ? "ProjectSelector_Open" : "ProjectSelector_Create";
        CreateOpenButtonText = LocalizationService.Instance.GetString(key);

        // If exists, make type non-editable
        IsProjectTypeEnabled = !exists;
    }
}