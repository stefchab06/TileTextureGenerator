using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using WinClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace TileTextureGenerator.Frontend.UI.Platforms.Windows;

/// <summary>
/// Windows-specific clipboard service using native Windows APIs
/// </summary>
public class WindowsClipboardService
{
    public static async Task<bool> HasImageAsync()
    {
        try
        {
            var dataPackageView = WinClipboard.GetContent();
            return dataPackageView.Contains(StandardDataFormats.Bitmap);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<byte[]?> GetImageAsync()
    {
        try
        {
            var dataPackageView = WinClipboard.GetContent();

            if (!dataPackageView.Contains(StandardDataFormats.Bitmap))
                return null;

            var bitmapReference = await dataPackageView.GetBitmapAsync();
            if (bitmapReference == null)
                return null;

            using var stream = await bitmapReference.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error getting image from Windows clipboard: {ex.Message}");
#endif
            return null;
        }
    }

    public static void StartMonitoring(Action onClipboardChanged)
    {
        // Windows doesn't provide a native clipboard change notification
        // We'll use polling in the service
    }
}
