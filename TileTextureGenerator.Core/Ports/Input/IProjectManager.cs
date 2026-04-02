using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Input;

/// <summary>
/// Input port for individual project management operations.
/// Implemented by ProjectBase to allow projects to manage their own state and persistence.
/// </summary>
public interface IProjectManager
{
    /// <summary>
    /// Type identifier for polymorphic instantiation and UI template selection.
    /// Typically the class name (e.g., "FloorTileProject").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Saves all changes made to the project.
    /// Updates LastModifiedDate automatically.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Adds a new transformation to the project and persists the change.
    /// </summary>
    /// <param name="transformation">The transformation metadata to add.</param>
    Task AddTransformationAsync(string transformationType);

    /// <summary>
    /// Removes a transformation from the project and persists the change.
    /// </summary>
    /// <param name="transformationId">ID of the transformation to remove.</param>
    Task RemoveTransformationAsync(Guid transformationId);

    /// <summary>
    /// Get a tranformation concrete instance.
    /// </summary>
    /// <param name="transformationId">ID of the transformation to get.</param>
    Task<TransformationBase> GetTransformationAsync(Guid transformationId);

    /// <summary>
    /// Gets the list of transformation types available for this project type.
    /// Returns metadata (name and icon) for each compatible transformation type.
    /// </summary>
    /// <returns>List of available transformation type metadata.</returns>
    Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync();

    /// <summary>
    /// Generate all transformations of the project and the final pdf file.
    /// </summary>
    /// <returns>True if generation succeded.</returns>
    Task<bool> GenerateAsync();


    /// <summary>
    /// Archive the project and all its transformations.
    /// </summary>
    /// <returns>True if Archive succeded.</returns>
    Task<bool> ArchiveAsync();


}
