using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.Ports.Input;

/// <summary>
/// Result from image initialization view
/// </summary>
public class ImageInitializationResult
{
    public ImageData? ImageData { get; set; }
    public TileShape TileShape { get; set; }
    public bool WasCancelled { get; set; }
}

/// <summary>
/// Service to navigate to image initialization view and get results
/// </summary>
public interface IImageInitializationService
{
    /// <summary>
    /// Opens the image initialization view and returns the result
    /// </summary>
    Task<ImageInitializationResult> InitializeImageAsync();
}
