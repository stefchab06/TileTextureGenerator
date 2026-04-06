using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace TileTextureGenerator.Platforms.Windows;

/// <summary>
/// Handles Windows close button (X) to prevent app closing when on EditProjectPage.
/// </summary>
public static class WindowsCloseHandler
{
    private static AppWindow? _appWindow;

    /// <summary>
    /// Initialize the close handler for the main window.
    /// Call this from MauiProgram.cs after window creation.
    /// </summary>
    public static void Initialize(Microsoft.Maui.Controls.Window mauiWindow)
    {
        // Get the native Windows window
        var nativeWindow = mauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow == null)
            return;

        // Get AppWindow from native window
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            // Subscribe to Closing event
            _appWindow.Closing += OnAppWindowClosing;
        }
    }

    /// <summary>
    /// Called when Windows close button (X) is clicked.
    /// </summary>
    private static async void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Check if we're on EditProjectPage
        if (Shell.Current?.CurrentPage?.GetType().Name == "EditProjectPage")
        {
            // Cancel the close operation
            args.Cancel = true;

            // Navigate back to ManageProjectListPage
            await Shell.Current.GoToAsync("//ManageProjectListPage");
        }
        // If we're on ManageProjectListPage or any other page, allow closing
    }
}
