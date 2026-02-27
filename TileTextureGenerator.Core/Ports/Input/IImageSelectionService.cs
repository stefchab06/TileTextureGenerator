namespace TileTextureGenerator.Core.Ports.Input;

/// <summary>
/// Service to select images from various sources (file system, clipboard, etc.)
/// Port defined in Core, implemented in UI layer
/// </summary>
public interface IImageSelectionService
{
    /// <summary>
    /// Opens a file picker to select an image from the file system
    /// </summary>
    /// <returns>Image data as byte array, or null if cancelled</returns>
    Task<byte[]?> PickImageFromFileAsync();
    
    // Future: Add more methods
    // Task<byte[]?> PickImageFromClipboardAsync();
    // Task<byte[]?> CaptureImageFromCameraAsync();
}
