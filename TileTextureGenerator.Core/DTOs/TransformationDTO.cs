using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.DTOs;

/// <summary>
/// Data transfer object representing a transformation instance in a project.
/// Contains metadata for displaying the transformation in the UI.
/// Order of transformations is determined by position in the list (no DisplayOrder property).
/// </summary>
public class TransformationDTO
{
    /// <summary>
    /// Unique identifier of the transformation instance.
    /// Used as selection key and for persistence operations.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Type name of the transformation (e.g., "HorizontalFloorTransformation").
    /// Used for polymorphic instantiation via TransformationTypeRegistry.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Icon representing this transformation type (PNG format, typically 64x64).
    /// Retrieved from TransformationTypeRegistry.GetIcon(Type) during loading.
    /// </summary>
    public ImageData? Icon { get; init; }
}
