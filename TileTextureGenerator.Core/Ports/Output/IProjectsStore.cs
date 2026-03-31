using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Output port for projects collection persistence operations.
/// Provides simple, technical persistence methods without business logic.
/// Implemented by persistence adapters (e.g., file system, database).
/// </summary>
public interface IProjectsStore
{
    /// <summary>
    /// Creates a new project in storage.
    /// Only persists basic DTO properties (name, type, status, lastModifiedDate).
    /// Does NOT handle transformations or complex polymorphic properties.
    /// </summary>
    /// <param name="projectDto">DTO containing basic project information.</param>
    Task CreateProjectAsync(ProjectDto projectDto);

    /// <summary>
    /// Loads a project by name from storage.
    /// </summary>
    /// <param name="projectName">Name of the project to load.</param>
    /// <returns>The loaded project, or null if not found.</returns>
    Task<ProjectBase?> LoadAsync(string projectName);

    /// <summary>
    /// Deletes a project from storage.
    /// </summary>
    /// <param name="projectName">Name of the project to delete.</param>
    Task DeleteAsync(string projectName);

    /// <summary>
    /// Checks whether a project exists in storage.
    /// </summary>
    /// <param name="projectName">Name of the project to check.</param>
    /// <returns>True if the project exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string projectName);

    /// <summary>
    /// Retrieves a list of all projects in storage.
    /// Returns lightweight DTOs for display purposes.
    /// </summary>
    /// <returns>Read-only list of project DTOs.</returns>
    Task<IReadOnlyList<ProjectDto>> ListProjectsAsync();

    /// <summary>
    /// Archives a project by removing temporary files and reducing JSON to essential properties.
    /// After archiving:
    /// - Workspace folder is deleted
    /// - JSON contains only base class properties (ProjectBase, TransformationBase)
    /// - PDF generation remains possible (GeneratedTexture preserved)
    /// - Transformation modification is disabled (EdgeFlap removed)
    /// </summary>
    /// <param name="projectName">Name of the project to archive.</param>
    Task ArchiveAsync(string projectName);
}

