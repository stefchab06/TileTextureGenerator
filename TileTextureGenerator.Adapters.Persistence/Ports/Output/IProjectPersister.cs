using TileTextureGenerator.Adapters.Persistence.Dto;

namespace TileTextureGenerator.Adapters.Persistence.Ports.Output;

public interface IProjectPersister
{
    public Task<Boolean> CreateProjectAsync(ProjectDataDto project);
    public Task<Boolean> DeleteProjectAsync(string projectName);
    public Task<ProjectDataDto> LoadProjectAsync(string projectName);
    public Task<Boolean> SaveProjectAsync(ProjectDataDto project);
    public Task<IList<ProjectDataDto>> GetProjectList();

    /// <summary>
    /// Get lightweight project summaries for list display (no full entity instantiation)
    /// </summary>
    public Task<IList<ProjectSummaryDto>> GetProjectSummariesAsync();

    /// <summary>
    /// Saves the source image to the Sources folder
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <param name="imageData">Image data (PNG format)</param>
    /// <param name="filename">Filename (e.g., "SourceImage.png")</param>
    /// <returns>Relative path from project root (e.g., "Sources\SourceImage.png")</returns>
    public Task<string> SaveSourceImageAsync(string projectName, byte[] imageData, string filename = "SourceImage.png");
}
