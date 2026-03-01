using TileTextureGenerator.Adapters.Persistence.Dto;
using TileTextureGenerator.Adapters.Persistence.Ports.Output;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Adapters.Persistence.ProjectManagement;

internal class TextureProjectStore : ITextureProjectStore
{
    private readonly IProjectPersister _persister;

    public TextureProjectStore(IProjectPersister persister)
    {
        _persister = persister;
    }

    public async Task<TileTextureProjectBase> CreateNewProjectAsync(TileTextureProjectBase project)
    {
        var p = new ProjectDataDto(project);
        if (await _persister.CreateProjectAsync(p))
        {
            // Load the project from the persisted JSON to ensure all properties are initialized
            return await LoadProjectFromFileAsync(project.Name);
        }
        else
        {
            return null;
        }
    }

    public async Task<TileTextureProjectBase> OpenProjectAsync(TileTextureProjectBase project)
    {
        return await LoadProjectFromFileAsync(project.Name);
    }

    /// <summary>
    /// Loads a project from file and initializes it with the JSON data
    /// </summary>
    private async Task<TileTextureProjectBase> LoadProjectFromFileAsync(string projectName)
    {
        var projectData = await _persister.LoadProjectAsync(projectName);

        if (!TextureProjectRegistry.IsRegistered(projectData.ProjectType))
        {
            return null;
        }

        // Create the project instance
        var project = TextureProjectRegistry.Create(projectData.ProjectType, projectData.ProjectName);

        // Load all properties from JSON (including custom properties)
        if (!string.IsNullOrEmpty(projectData.ProjectDataJson))
        {
            project.LoadFromJson(projectData.ProjectDataJson);
        }
        else
        {
            // Fallback: just set the status if no JSON available
            if (Enum.TryParse<ProjectStatus>(projectData.ProjectStatus, out var ps))
            {
                project.Status = ps;
            }
        }

        // Map DisplayImage from persistence to domain
        if (projectData.DisplayImage != null)
        {
            project.DisplayImage = projectData.DisplayImage;
        }

        // Map SourceImage from persistence to domain (if HorizontalTileTextureProject)
        if (project is HorizontalTileTextureProjectEntity horizontalProject && projectData.SourceImage != null)
        {
            horizontalProject.SourceImage = projectData.SourceImage;
        }

        return project;
    }
    public async Task<TileTextureProjectBase> ArchiveProjectAsync(TileTextureProjectBase project)
    {
        throw new NotImplementedException();
    }
    public async Task<bool> DeleteProjectAsync(string projectName)
    {
        return await _persister.DeleteProjectAsync(projectName);
    }

    public async Task<IReadOnlyList<TileTextureProjectSummary>> LoadProjectSummariesAsync()
    {
        var summaries = await _persister.GetProjectSummariesAsync();

        return summaries.Select(dto => new TileTextureProjectSummary(
            name: dto.ProjectName,
            type: dto.ProjectType,
            status: dto.ProjectStatus,
            lastModifiedDate: dto.LastModifiedDate,
            displayImage: dto.DisplayImage
        )).ToList();
    }

    public async Task<IReadOnlyList<TileTextureProjectBase>> LoadProjectListAsync()
    {
        var lst = await _persister.GetProjectList();
        return lst.Select(p => {
            var proj = TextureProjectRegistry.Create(p.ProjectType, p.ProjectName);
            proj.DisplayImage = p.DisplayImage;
            // Load all properties from JSON
            if (!string.IsNullOrEmpty(p.ProjectDataJson))
            {
                proj.LoadFromJson(p.ProjectDataJson);
            }
            else if (Enum.TryParse<ProjectStatus>(p.ProjectStatus, out var ps))
            {
                proj.Status = ps;
            }

            return proj;
        }).ToList();
    }

    public async Task<bool> SaveProjectAsync(TileTextureProjectBase project)
    {
        var dto = new ProjectDataDto(project);

        // If SourceImage exists, save it to file and store the path
        if (dto.SourceImage != null && dto.SourceImage.Length > 0)
        {
            dto.SourceImageFile = await _persister.SaveSourceImageAsync(
                project.Name, 
                dto.SourceImage, 
                "SourceImage.png");
        }

        return await _persister.SaveProjectAsync(dto);
    }
}
