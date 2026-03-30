using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.Ports.Input;

/// <summary>
/// Input port for individual transformation management operations.
/// Implemented by TransformationBase to allow transformations to manage their own state and persistence.
/// </summary>
public interface ITransformationManager
{
    /// <summary>
    /// Gets the type of paper required for printing this transformation output.
    /// Used to group transformations for separate print jobs.
    /// </summary>
    Enums.PaperType RequiredPaperType { get; }

    /// <summary>
    /// Saves all changes made to the transformation.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Generate output image based on concrete implementation of the transformation.
    /// </summary>
    /// <returns>The generated image as PNG.</returns>
    Task<ImageData> GenerateAsync();
}
