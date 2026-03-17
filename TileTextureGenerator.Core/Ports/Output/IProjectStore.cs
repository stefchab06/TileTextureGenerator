using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Output port for individual project persistence operations.
/// Non-generic interface for polymorphic project persistence.
/// Implemented by persistence adapters (e.g., file system, database).
/// Handles saving project changes from entity instances.
/// Loading is handled by IProjectsStore at the application level.
/// </summary>
public interface IProjectStore
{
    /// <summary>
    /// Persists a project instance to storage.
    /// The concrete type will be serialized polymorphically.
    /// </summary>
    /// <param name="project">The project to save.</param>
    Task SaveAsync(ProjectBase project);
}
