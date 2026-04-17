namespace TileTextureGenerator.Presentation.UI.Controls.ImageCropping;

/// <summary>
/// Math helpers for image cropping calculations.
/// Pure functions with no dependencies - easily testable and reusable.
/// </summary>
public static class CroppingMath
{
    /// <summary>
    /// Calculates the zoom and offset to fit an image within a cropping polygon using "fit-to-fill" logic.
    /// The image is scaled to completely fill the polygon's bounding box, potentially overflowing one dimension.
    /// The image is centered on the polygon's bounding box.
    /// </summary>
    /// <param name="imageSize">Size of the source image (in pixels).</param>
    /// <param name="canvasSize">Size of the canvas (unused in current implementation, for future viewport clipping).</param>
    /// <param name="polygon">Cropping polygon points (in screen coordinates, typically pixels).</param>
    /// <returns>Zoom factor and top-left offset for rendering the image.</returns>
    /// <exception cref="ArgumentNullException">Thrown if polygon is null.</exception>
    /// <exception cref="ArgumentException">Thrown if polygon has less than 3 points or image size is invalid.</exception>
    public static (double Zoom, PointF Offset) CalculateFitToFill(
        SizeF imageSize,
        SizeF canvasSize,
        IReadOnlyList<Point> polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (polygon.Count < 3)
            throw new ArgumentException("Polygon must have at least 3 points.", nameof(polygon));

        if (imageSize.Width <= 0 || imageSize.Height <= 0)
            throw new ArgumentException("Image size must have positive dimensions.", nameof(imageSize));

        // 1. Calculate bounding box (circumscribed rectangle) of the polygon
        double minX = polygon.Min(p => p.X);
        double minY = polygon.Min(p => p.Y);
        double maxX = polygon.Max(p => p.X);
        double maxY = polygon.Max(p => p.Y);

        double croppingWidth = maxX - minX;   // CW
        double croppingHeight = maxY - minY;  // CH

        // 2. Calculate zoom to fill the bounding box
        // Use MAX so the image fills completely (one dimension fits exactly, the other overflows)
        double zoomWidth = croppingWidth / imageSize.Width;   // CW / PW
        double zoomHeight = croppingHeight / imageSize.Height; // CH / PH
        double zoom = Math.Max(zoomWidth, zoomHeight);

        // 3. Calculate scaled image dimensions
        double scaledWidth = imageSize.Width * zoom;
        double scaledHeight = imageSize.Height * zoom;

        // 4. Center the image on the bounding box
        // If the image overflows, it will overflow equally on both sides
        double offsetX = minX + (croppingWidth - scaledWidth) / 2.0;
        double offsetY = minY + (croppingHeight - scaledHeight) / 2.0;

        return (zoom, new PointF((float)offsetX, (float)offsetY));
    }
}
