using UIKit;
using Foundation;

namespace TileTextureGenerator.Frontend.UI.Platforms.MacCatalyst;

/// <summary>
/// macOS Catalyst-specific clipboard service using native APIs
/// </summary>
public class MacClipboardService
{
    public static async Task<bool> HasImageAsync()
    {
        try
        {
            var pasteboard = UIPasteboard.General;
            return pasteboard.HasImages;
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
            var pasteboard = UIPasteboard.General;
            if (!pasteboard.HasImages)
                return null;

            var image = pasteboard.Image;
            if (image == null)
                return null;

            using var data = image.AsPNG();
            if (data == null)
                return null;

            var bytes = new byte[data.Length];
            System.Runtime.InteropServices.Marshal.Copy(data.Bytes, bytes, 0, (int)data.Length);
            return bytes;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error getting image from Mac clipboard: {ex.Message}");
#endif
            return null;
        }
    }

    public static void StartMonitoring(Action onClipboardChanged)
    {
        NSNotificationCenter.DefaultCenter.AddObserver(
            UIPasteboard.ChangedNotification,
            notification => onClipboardChanged?.Invoke());
    }
}
