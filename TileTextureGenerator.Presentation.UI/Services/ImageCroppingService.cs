namespace TileTextureGenerator.Presentation.UI.Services;

/// <summary>
/// Service for displaying the image cropping editor and managing navigation.
/// Orchestrates the flow: navigate to ImageCroppingPage → wait for result → return cropped image.
/// </summary>
public class ImageCroppingService
{
    // TaskCompletionSource to wait for user's result (validate/cancel)
    private TaskCompletionSource<byte[]?>? _resultCompletionSource;

    // Temporary storage for navigation parameters (cleared after navigation)
    private IReadOnlyList<Point>? _currentCroppingPolygon;
    private byte[]? _currentInitialImage;

    /// <summary>
    /// Shows the image cropping editor.
    /// </summary>
    /// <param name="croppingPolygon">Polygon points in proportional coordinates (0-1 or any consistent scale).</param>
    /// <param name="initialImage">Existing image to edit (optional).</param>
    /// <returns>Cropped image (PNG with transparency) or null if cancelled.</returns>
    public async Task<byte[]?> ShowCroppingEditorAsync(
        IReadOnlyList<Point> croppingPolygon,
        byte[]? initialImage = null)
    {
        ArgumentNullException.ThrowIfNull(croppingPolygon);

        if (croppingPolygon.Count < 3)
            throw new ArgumentException("Cropping polygon must have at least 3 points.", nameof(croppingPolygon));

        // Store parameters temporarily (will be retrieved by the page)
        _currentCroppingPolygon = croppingPolygon;
        _currentInitialImage = initialImage;

        // Create a new TaskCompletionSource for this operation
        _resultCompletionSource = new TaskCompletionSource<byte[]?>();

        try
        {
            // Navigate to ImageCroppingPage (no parameters passed via Shell)
            await Shell.Current.GoToAsync("ImageCroppingPage");

            // Wait for user to validate or cancel
            return await _resultCompletionSource.Task;
        }
        catch
        {
            // Navigation failed or operation cancelled
            _resultCompletionSource = null;
            _currentCroppingPolygon = null;
            _currentInitialImage = null;
            return null;
        }
    }

    /// <summary>
    /// Gets the current cropping polygon (called by ImageCroppingPage during initialization).
    /// </summary>
    internal IReadOnlyList<Point>? GetCurrentCroppingPolygon()
    {
        return _currentCroppingPolygon;
    }

    /// <summary>
    /// Gets the current initial image (called by ImageCroppingPage during initialization).
    /// </summary>
    internal byte[]? GetCurrentInitialImage()
    {
        return _currentInitialImage;
    }

    /// <summary>
    /// Called by ImageCroppingPage when user validates the cropped image.
    /// </summary>
    internal void CompleteWithResult(byte[] croppedImage)
    {
        _resultCompletionSource?.SetResult(croppedImage);
        _resultCompletionSource = null;
        _currentCroppingPolygon = null;
        _currentInitialImage = null;
    }

    /// <summary>
    /// Called by ImageCroppingPage when user cancels.
    /// </summary>
    internal void CompleteWithCancellation()
    {
        _resultCompletionSource?.SetResult(null);
        _resultCompletionSource = null;
        _currentCroppingPolygon = null;
        _currentInitialImage = null;
    }
}
