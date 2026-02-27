using TileTextureGenerator.Adapters.UseCases.Dto;
using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Adapters.UseCases.Registries;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Adapters.UseCases.ProjectManagers;

internal class ProjectSelectionUseCase: IProjectSelectionUseCase
{
    private readonly ITextureProjectStore _ProjectStore;

    public ProjectSelectionUseCase(ITextureProjectStore projectStore)
    {
        _ProjectStore = projectStore;
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

            // Get the workflow for this project type
            var workflow = WorkflowRegistry.GetWorkflow(createdProject);

            // Initialize the project using its specific workflow
            await workflow.InitializeAsync(createdProject);

            // Save after workflow execution
            await _ProjectStore.SaveProjectAsync(createdProject);
        }

        return ToProjectDto(createdProject);
    }

    public async Task<TextureProjectDto> OpenAsync(TextureProjectDto project)
    {
        // Load the full project from storage (not just create a new empty one)
        var tempProject = TextureProjectRegistry.Create(project.Type, project.Name);
        var openedProject = await _ProjectStore.OpenProjectAsync(tempProject);

        if (openedProject != null)
        {
            // Get the workflow for this project type
            var workflow = WorkflowRegistry.GetWorkflow(openedProject);

            // Initialize or continue based on project status
            if (openedProject.Status == ProjectStatus.New)
            {
                await workflow.InitializeAsync(openedProject);
            }
            else
            {
                await workflow.ContinueWorkAsync(openedProject);
            }

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

    public async Task<IReadOnlyList<TextureProjectSummaryDto>> GetProjectSummariesAsync()
    {
        var summaries = await _ProjectStore.LoadProjectSummariesAsync();

        // Sort by LastModifiedDate descending (most recent first)
        return summaries
            .Select(s => new TextureProjectSummaryDto(
                name: s.Name,
                type: s.Type,
                status: s.Status,
                lastModifiedDate: s.LastModifiedDate,
                displayImage: s.DisplayImage))
            .OrderByDescending(s => s.LastModifiedDate)
            .ToList();
    }
}
