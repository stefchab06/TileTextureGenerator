using Android.Content;
using Android.Graphics;
using AndroidX.Core.Content;

namespace TileTextureGenerator.Frontend.UI.Platforms.Android;

/// <summary>
/// Android-specific clipboard service using native Android APIs
/// </summary>
public class AndroidClipboardService
{
    private static ClipboardManager? _clipboardManager;

    private static ClipboardManager GetClipboardManager()
    {
        if (_clipboardManager == null)
        {
            var context = Platform.CurrentActivity ?? Platform.AppContext;
            _clipboardManager = (ClipboardManager?)context.GetSystemService(Context.ClipboardService);
        }
        return _clipboardManager!;
    }

    public static async Task<bool> HasImageAsync()
    {
        try
        {
            var clipboardManager = GetClipboardManager();
            if (clipboardManager?.PrimaryClip == null)
                return false;

            var item = clipboardManager.PrimaryClip.GetItemAt(0);
            return item?.Uri != null && IsImageUri(item.Uri);
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
            var clipboardManager = GetClipboardManager();
            if (clipboardManager?.PrimaryClip == null)
                return null;

            var item = clipboardManager.PrimaryClip.GetItemAt(0);
            if (item?.Uri == null)
                return null;

            var context = Platform.CurrentActivity ?? Platform.AppContext;
            using var inputStream = context.ContentResolver?.OpenInputStream(item.Uri);
            if (inputStream == null)
                return null;

            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error getting image from Android clipboard: {ex.Message}");
#endif
            return null;
        }
    }

    private static bool IsImageUri(global::Android.Net.Uri uri)
    {
        var mimeType = Platform.AppContext.ContentResolver?.GetType(uri);
        return mimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static void StartMonitoring(Action onClipboardChanged)
    {
        var clipboardManager = GetClipboardManager();
        clipboardManager.AddPrimaryClipChangedListener(new ClipboardChangeListener(onClipboardChanged));
    }

    private class ClipboardChangeListener : Java.Lang.Object, ClipboardManager.IOnPrimaryClipChangedListener
    {
        private readonly Action _onClipboardChanged;

        public ClipboardChangeListener(Action onClipboardChanged)
        {
            _onClipboardChanged = onClipboardChanged;
        }

        public void OnPrimaryClipChanged()
        {
            _onClipboardChanged?.Invoke();
        }
    }
}
