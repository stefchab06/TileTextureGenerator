namespace TileTextureGenerator.Core.Ports.Input;

/// <summary>
/// Input port for individual transformation management operations.
/// Implemented by TransformationBase to allow transformations to manage their own state and persistence.
/// </summary>
public interface ITransformationManager
{
    /// <summary>
    /// Saves all changes made to the transformation.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Executes the transformation and generates the output image.
    /// </summary>
    /// <returns>The generated image as PNG byte array.</returns>
    Task<byte[]> ExecuteAsync();
}
