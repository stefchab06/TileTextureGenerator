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

    /// <summary>
    /// Gets image from clipboard if available
    /// </summary>
    /// <returns>Image data as byte array, or null if no image in clipboard</returns>
    Task<byte[]?> GetImageFromClipboardAsync();

    /// <summary>
    /// Checks if clipboard contains an image
    /// </summary>
    /// <returns>True if clipboard has an image</returns>
    Task<bool> HasImageInClipboardAsync();

    /// <summary>
    /// Checks if device has scanning/camera capabilities
    /// </summary>
    /// <returns>True if device can scan/capture images</returns>
    Task<bool> CanScanOrCaptureAsync();

    /// <summary>
    /// Subscribe to clipboard content changes
    /// </summary>
    /// <param name="callback">Action to invoke when clipboard changes</param>
    void OnClipboardChanged(Action callback);
}
