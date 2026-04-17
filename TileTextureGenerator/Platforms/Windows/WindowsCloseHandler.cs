using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace TileTextureGenerator.Platforms.Windows;

/// <summary>
/// Handles Windows close button (X) behavior.
/// - On ManageProjectListPage: Closes the application.
/// - On any other page: Navigates back to previous page.
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
    /// Default behavior: Navigate back to previous page.
    /// Exception: On ManageProjectListPage, close the application.
    /// </summary>
    private static async void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var currentPageType = Shell.Current?.CurrentPage?.GetType().Name;

        // Only allow closing if we're on the main page (ManageProjectListPage)
        if (currentPageType == "ManageProjectListPage")
        {
            // Allow closing the application
            return;
        }

        // For all other pages, cancel the close operation and navigate back
        args.Cancel = true;

        // Navigate back to previous page
        await Shell.Current.GoToAsync("..");
    }
}
