using TileTextureGenerator.Core.Ports.Input;

namespace TileTextureGenerator.Frontend.UI.Services;

/// <summary>
/// MAUI implementation of image selection service with platform-specific clipboard support
/// </summary>
public class ImageSelectionService : IImageSelectionService
{
    private System.Threading.Timer? _clipboardMonitorTimer;
    private event Action? ClipboardChanged;

    public ImageSelectionService()
    {
        StartClipboardMonitoring();
    }

    public async Task<byte[]?> PickImageFromFileAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select an image",
                FileTypes = FilePickerFileType.Images
            });

            if (result == null)
                return null;

            // Read the file into a byte array
            using var stream = await result.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error picking image: {ex.Message}");
#endif
            return null;
        }
    }

    public async Task<byte[]?> GetImageFromClipboardAsync()
    {
#if WINDOWS
        return await Platforms.Windows.WindowsClipboardService.GetImageAsync();
#elif ANDROID
        return await Platforms.Android.AndroidClipboardService.GetImageAsync();
#elif IOS
        return await Platforms.iOS.iOSClipboardService.GetImageAsync();
#elif MACCATALYST
        return await Platforms.MacCatalyst.MacClipboardService.GetImageAsync();
#else
        return null;
#endif
    }

    public async Task<bool> HasImageInClipboardAsync()
    {
#if WINDOWS
        return await Platforms.Windows.WindowsClipboardService.HasImageAsync();
#elif ANDROID
        return await Platforms.Android.AndroidClipboardService.HasImageAsync();
#elif IOS
        return await Platforms.iOS.iOSClipboardService.HasImageAsync();
#elif MACCATALYST
        return await Platforms.MacCatalyst.MacClipboardService.HasImageAsync();
#else
        return false;
#endif
    }

    public async Task<bool> CanScanOrCaptureAsync()
    {
        try
        {
            // Check if device has camera
            return await Task.FromResult(
                MediaPicker.Default.IsCaptureSupported);
        }
        catch
        {
            return false;
        }
    }

    public void OnClipboardChanged(Action callback)
    {
        ClipboardChanged += callback;
    }

    private void StartClipboardMonitoring()
    {
#if ANDROID
        Platforms.Android.AndroidClipboardService.StartMonitoring(() =>
        {
            MainThread.BeginInvokeOnMainThread(() => ClipboardChanged?.Invoke());
        });
#elif IOS
        Platforms.iOS.iOSClipboardService.StartMonitoring(() =>
        {
            MainThread.BeginInvokeOnMainThread(() => ClipboardChanged?.Invoke());
        });
#elif MACCATALYST
        Platforms.MacCatalyst.MacClipboardService.StartMonitoring(() =>
        {
            MainThread.BeginInvokeOnMainThread(() => ClipboardChanged?.Invoke());
        });
#elif WINDOWS
        // Windows doesn't have native clipboard change events, use polling
        _clipboardMonitorTimer = new System.Threading.Timer(async _ =>
        {
            await MainThread.InvokeOnMainThreadAsync(() => ClipboardChanged?.Invoke());
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
#endif
    }
}
