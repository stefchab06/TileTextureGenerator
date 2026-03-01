using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;

namespace TileTextureGenerator.Frontend.UI.Services;

/// <summary>
/// Service to handle navigation to image initialization view
/// Uses a static TaskCompletionSource to wait for results (MAUI Shell pattern)
/// </summary>
public class ImageInitializationService : IImageInitializationService
{
    private static TaskCompletionSource<ImageInitializationResult>? _resultSource;

    public async Task<ImageInitializationResult> InitializeImageAsync()
    {
        _resultSource = new TaskCompletionSource<ImageInitializationResult>();

        // Navigate to the initialization view
        await Shell.Current.GoToAsync("SingleTileTextureInitializationView");

        // Wait for the result
        return await _resultSource.Task;
    }

    /// <summary>
    /// Called by the view when user validates or cancels
    /// </summary>
    public static void CompleteInitialization(ImageInitializationResult result)
    {
        _resultSource?.TrySetResult(result);
    }
}
