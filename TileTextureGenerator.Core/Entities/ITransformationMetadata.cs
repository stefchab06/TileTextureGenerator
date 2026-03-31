namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Interface defining metadata requirements for transformation types.
/// Ensures all concrete transformation classes provide necessary metadata like icons.
/// Uses static abstract members to enforce compile-time guarantees.
/// </summary>
public interface ITransformationMetadata
{
    /// <summary>
    /// Resource name for the transformation icon (must be an embedded resource).
    /// Format: "Icons.{TransformationType}.png" (e.g., "Icons.HorizontalFloor.png")
    /// The resource must exist in TileTextureGenerator.Core/Resources/Icons/ and be marked as EmbeddedResource.
    /// Compilation will fail if concrete classes do not implement this property.
    /// </summary>
    abstract static string IconResourceName { get; }
}
