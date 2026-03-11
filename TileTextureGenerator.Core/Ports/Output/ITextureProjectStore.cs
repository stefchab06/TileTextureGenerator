using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Output port for project persistence operations.
/// Provides simple, technical persistence methods without business logic.
/// Implemented by persistence adapters (e.g., file system, database).
/// </summary>
public interface ITextureProjectStore
{
    /// <summary>
    /// Persists a project to storage.
    /// </summary>
    /// <param name="project">The project to save.</param>
    Task SaveAsync(ProjectBase project);

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
}

