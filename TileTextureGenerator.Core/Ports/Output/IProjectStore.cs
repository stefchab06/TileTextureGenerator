using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Output port for individual project persistence operations.
/// Non-generic interface for polymorphic project persistence.
/// Implemented by persistence adapters (e.g., file system, database).
/// </summary>
public interface IProjectStore
{
    /// <summary>
    /// Persists a project instance to storage.
    /// The concrete type will be serialized polymorphically.
    /// </summary>
    /// <param name="project">The project to save.</param>
    Task SaveAsync(ProjectBase project);

    /// <summary>
    /// Loads a project instance from storage.
    /// Returns the concrete type based on stored metadata.
    /// </summary>
    /// <param name="projectName">Name of the project to load.</param>
    /// <returns>The loaded project, or null if not found.</returns>
    Task<ProjectBase?> LoadAsync(string projectName);
}
