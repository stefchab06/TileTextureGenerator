using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.DTOs;

/// <summary>
/// Data transfer object representing a transformation type available for a project.
/// Contains the type name and its associated icon.
/// </summary>
public class TransformationTypeDTO
{
    /// <summary>
    /// Name of the transformation type (e.g., "HorizontalFloorTransformation").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Icon representing this transformation type (PNG format, typically 64x64).
    /// </summary>
    public ImageData? Icon { get; init; }
}
