using TileTextureGenerator.Adapters.UseCases.Dto;
using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Adapters.UseCases.ProjectManagers;

internal class ProjectSelectionUseCase: IProjectSelectionUseCase
{
    private readonly ITextureProjectStore _ProjectStore;
    private readonly IHorizontalTileTextureWorkflow _horizontalTileWorkflow;

    public ProjectSelectionUseCase(ITextureProjectStore projectStore, IHorizontalTileTextureWorkflow horizontalTileWorkflow)
    {
        _ProjectStore = projectStore;
        _horizontalTileWorkflow = horizontalTileWorkflow;
    }
    private TileTextureProjectBase ToCoreProject(TextureProjectDto project)
    {
        var proj = TextureProjectRegistry.Create(project.Type, project.Name);
        if (Enum.TryParse<ProjectStatus>(project.Status, out var status))
        {
            proj.Status = status;
        }
        if (project.DisplayImage != null)
        {
            proj.DisplayImage = project.DisplayImage;
        }
        return proj;
    }
    private TextureProjectDto ToProjectDto(TileTextureProjectBase project)
    {
        var status = project.Status.ToString();
        return new TextureProjectDto(project.Name, project.Type, status)
        {
            DisplayImage = project.DisplayImage,  // Map the image data
            LastModifiedDate = project.LastModifiedDate
        };
    }
    public async Task<TextureProjectDto> CreateAsync(TextureProjectDto project)
    {
        // Create the project with initial status
        var createdProject = await _ProjectStore.CreateNewProjectAsync(ToCoreProject(project));

        if (createdProject != null)
        {
            // Change status to "New" after successful creation
            createdProject.Status = ProjectStatus.New;

            // Update last modified date
            createdProject.LastModifiedDate = DateTime.UtcNow;

            // Save the updated project with new status
            await _ProjectStore.SaveProjectAsync(createdProject);

            // Start the project workflow and get the action to execute
            var action = await createdProject.StartAsync();

            // Orchestrate: execute the appropriate workflow use case
            await ExecuteWorkflowActionAsync(action, createdProject);

            // Save after workflow execution
            await _ProjectStore.SaveProjectAsync(createdProject);
        }

        return ToProjectDto(createdProject);
    }
    public async Task<TextureProjectDto> OpenAsync(TextureProjectDto project)
    {
        var openedProject = await _ProjectStore.OpenProjectAsync(ToCoreProject(project));

        if (openedProject != null)
        {
            WorkflowAction action;

            // Start workflow if status is New, otherwise continue existing workflow
            if (openedProject.Status == ProjectStatus.New)
            {
                action = await openedProject.StartAsync();
            }
            else
            {
                action = await openedProject.ContinueAsync();
            }

            // Orchestrate: execute the appropriate workflow use case
            await ExecuteWorkflowActionAsync(action, openedProject);

            // Save after workflow execution
            await _ProjectStore.SaveProjectAsync(openedProject);
        }

        return ToProjectDto(openedProject);
    }
    public async Task<TextureProjectDto> ArchiveAsync(TextureProjectDto project)
    {
        var coreProject = await _ProjectStore.ArchiveProjectAsync(ToCoreProject(project));

        if (coreProject != null)
        {
            // Update last modified date
            coreProject.LastModifiedDate = DateTime.UtcNow;
            await _ProjectStore.SaveProjectAsync(coreProject);
        }

        return ToProjectDto(coreProject);
    }
    public async Task<bool> DeleteAsync(TextureProjectDto project)
    {
        return await _ProjectStore.DeleteProjectAsync(project.Name);
    }

    public async Task<bool> SaveAsync(TextureProjectDto project)
    {
        var coreProject = ToCoreProject(project);

        // Update last modified date
        coreProject.LastModifiedDate = DateTime.UtcNow;

        return await _ProjectStore.SaveProjectAsync(coreProject);
    }

    public async Task<IReadOnlyList<string>> GetProjectTypeListAsync()
    {
        return TextureProjectRegistry.GetRegisteredType();
    }
    public List<string> GetProjectStatusList()
    {
        var values = Enum.GetValues<ProjectStatus>();
        var list = values.Select(v => v.ToString());
        return list.ToList();
    }
    public async Task<IReadOnlyList<TextureProjectDto>> GetProjectListAsync()
    {
        var projects = await _ProjectStore.LoadProjectListAsync();

        // Sort by LastModifiedDate descending (most recent first)
        return projects
            .Select(p => ToProjectDto(p))
            .OrderByDescending(p => p.LastModifiedDate)
            .ToList();
    }

    /// <summary>
    /// Orchestrates workflow execution based on the WorkflowAction returned by the entity
    /// This respects hexagonal architecture: Core returns action, Adapters orchestrate
    /// </summary>
    private async Task ExecuteWorkflowActionAsync(WorkflowAction action, TileTextureProjectBase project)
    {
        switch (action)
        {
            case WorkflowAction.StartHorizontalTileGeneration:
                if (project is HorizontalTileTextureProjectEntity horizontalProject)
                {
                    await _horizontalTileWorkflow.StartGenerationAsync(horizontalProject);
                }
                break;

            case WorkflowAction.ContinueHorizontalTileGeneration:
                if (project is HorizontalTileTextureProjectEntity horizontalContinue)
                {
                    await _horizontalTileWorkflow.ContinueGenerationAsync(horizontalContinue);
                }
                break;

            case WorkflowAction.None:
            default:
                // No workflow action needed
                break;
        }
    }
}
