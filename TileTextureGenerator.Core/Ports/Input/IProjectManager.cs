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
    /// Saves all changes made to the project.
    /// Updates LastModifiedDate automatically.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Adds a new transformation to the project and persists the change.
    /// </summary>
    /// <param name="transformation">The transformation to add.</param>
    Task AddTransformationAsync(TransformationEntity transformation);

    /// <summary>
    /// Removes a transformation from the project and persists the change.
    /// </summary>
    /// <param name="transformationId">ID of the transformation to remove.</param>
    Task RemoveTransformationAsync(Guid transformationId);

    /// <summary>
    /// Reorders transformations in the project and persists the change.
    /// </summary>
    /// <param name="newOrder">New order of transformation IDs.</param>
    Task ReorderTransformationsAsync(IReadOnlyList<Guid> newOrder);

    /// <summary>
    /// Gets the list of transformation types available for this project type.
    /// Returns metadata (name and icon) for each compatible transformation type.
    /// </summary>
    /// <returns>List of available transformation type metadata.</returns>
    Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync();
}
