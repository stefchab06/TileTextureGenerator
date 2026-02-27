namespace TileTextureGenerator.Core.Services;

/// <summary>
/// Image processing service - Core business logic
/// Handles image format conversion and resizing
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Converts any image format to PNG, preserving original resolution
    /// </summary>
    byte[] ConvertToPng(byte[] imageData);
    
    /// <summary>
    /// Converts and resizes image to PNG at specified dimensions
    /// </summary>
    byte[] ConvertToPng(byte[] imageData, int width, int height);
}
