using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Input;

/// <summary>
/// Input port for projects collection management operations.
/// Called by UI adapters to manage the projects lifecycle.
/// </summary>
public interface IProjectsManager
{
    /// <summary>
    /// Retrieves a list of all existing projects.
    /// </summary>
    /// <returns>Read-only list of project summaries.</returns>
    Task<IReadOnlyList<ProjectDto>> ListProjectsAsync();

    /// <summary>
    /// Creates a new project with the specified name and type.
    /// </summary>
    /// <param name="name">Unique name for the project.</param>
    /// <param name="type">Type identifier registered in TextureProjectRegistry.</param>
    /// <returns>The newly created project entity.</returns>
    Task<ProjectBase> CreateProjectAsync(string name, string type);

    /// <summary>
    /// Selects and loads a project by name.
    /// </summary>
    /// <param name="name">Name of the project to load.</param>
    /// <returns>The loaded project entity.</returns>
    Task<ProjectBase> SelectProjectAsync(string name);

    /// <summary>
    /// Deletes a project by name.
    /// </summary>
    /// <param name="name">Name of the project to delete.</param>
    Task DeleteProjectAsync(string name);

    /// <summary>
    /// Retrieves the list of available project types.
    /// </summary>
    /// <returns>Read-only list of registered project type identifiers.</returns>
    Task<IReadOnlyList<string>> ListProjectTypesAsync();

    /// <summary>
    /// Checks whether a project with the specified name exists.
    /// </summary>
    /// <param name="projectName">Name of the project to check.</param>
    /// <returns>True if the project exists, false otherwise.</returns>
    Task<bool> ProjectExistsAsync(string projectName);

    /// <summary>
    /// Archives a project by removing temporary files and reducing storage to essential properties.
    /// After archiving:
    /// - Status is set to Archived
    /// - Workspace folder is deleted
    /// - JSON contains only base class properties
    /// - PDF generation remains possible (GeneratedTexture preserved)
    /// - Transformation modification is disabled (EdgeFlap removed)
    /// </summary>
    /// <param name="projectName">Name of the project to archive.</param>
    /// <exception cref="ArgumentNullException">Thrown when projectName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when projectName is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when project does not exist.</exception>
    Task ArchiveProjectAsync(string projectName);
}
