using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.DTOs;

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
    /// <summary>
    /// Add a new transformation in a project transformation list.
    /// </summary>
    /// <param name="project">The project where the transformation is added.</param>
    /// <param name="project">The project to transformation to add.</param>
    Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation);
    /// <summary>
    /// Remove a transformation in a project transformation list
    /// </summary>
    /// <param name="project">The project where the transformation is added.</param>
    /// <param name="project">The project to transformation to add.</param>
    Task RemoveTransformationAsync(ProjectBase project, Guid transformationID);


}
