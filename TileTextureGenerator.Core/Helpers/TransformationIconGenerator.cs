using SkiaSharp;

namespace TileTextureGenerator.Core.Helpers;

/// <summary>
/// Helper class to generate transformation icons programmatically.
/// Icons are generated using SkiaSharp to ensure consistent appearance.
/// </summary>
internal static class TransformationIconGenerator
{
    /// <summary>
    /// Generates icon for horizontal floor tile transformation.
    /// Shows a flat tile viewed from above (blue top surface, yellow side).
    /// </summary>
    public static byte[] GenerateHorizontalFloorIcon()
    {
        const int size = 64;
        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.Transparent);

        // Draw perspective tile
        // Blue top surface (main rectangle)
        using (var bluePaint = new SKPaint { Color = new SKColor(0, 150, 255), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            var topRect = new SKRect(8, 8, 48, 40);
            canvas.DrawRect(topRect, bluePaint);
        }

        // Yellow right side (perspective depth)
        using (var yellowPaint = new SKPaint { Color = new SKColor(255, 220, 0), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            var path = new SKPath();
            path.MoveTo(48, 8);   // Top right of blue
            path.LineTo(56, 12);  // Top right corner
            path.LineTo(56, 44);  // Bottom right corner
            path.LineTo(48, 40);  // Bottom right of blue
            path.Close();
            canvas.DrawPath(path, yellowPaint);
        }

        // Yellow bottom side (perspective depth)
        using (var yellowPaint = new SKPaint { Color = new SKColor(255, 200, 0), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            var path = new SKPath();
            path.MoveTo(8, 40);   // Bottom left of blue
            path.LineTo(48, 40);  // Bottom right of blue
            path.LineTo(56, 44);  // Bottom right corner
            path.LineTo(16, 44);  // Bottom left corner
            path.Close();
            canvas.DrawPath(path, yellowPaint);
        }

        // Black outline
        using (var outlinePaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true })
        {
            var topRect = new SKRect(8, 8, 48, 40);
            canvas.DrawRect(topRect, outlinePaint);

            var path = new SKPath();
            path.MoveTo(48, 8);
            path.LineTo(56, 12);
            path.LineTo(56, 44);
            path.LineTo(16, 44);
            path.LineTo(8, 40);
            canvas.DrawPath(path, outlinePaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    /// <summary>
    /// Generates icon for vertical wall tile transformation.
    /// Shows a tile viewed from the side for vertical wall mounting.
    /// </summary>
    public static byte[] GenerateVerticalWallIcon()
    {
        const int size = 64;
        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.Transparent);

        // Draw vertical tile perspective
        // Blue front surface (vertical rectangle)
        using (var bluePaint = new SKPaint { Color = new SKColor(0, 150, 255), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            var frontRect = new SKRect(16, 8, 40, 48);
            canvas.DrawRect(frontRect, bluePaint);
        }

        // Yellow right side (depth)
        using (var yellowPaint = new SKPaint { Color = new SKColor(255, 220, 0), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            var path = new SKPath();
            path.MoveTo(40, 8);   // Top right of blue
            path.LineTo(48, 12);  // Top right corner
            path.LineTo(48, 52);  // Bottom right corner
            path.LineTo(40, 48);  // Bottom right of blue
            path.Close();
            canvas.DrawPath(path, yellowPaint);
        }

        // Yellow bottom side
        using (var yellowPaint = new SKPaint { Color = new SKColor(255, 200, 0), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            var path = new SKPath();
            path.MoveTo(16, 48);  // Bottom left of blue
            path.LineTo(40, 48);  // Bottom right of blue
            path.LineTo(48, 52);  // Bottom right corner
            path.LineTo(24, 52);  // Bottom left corner
            path.Close();
            canvas.DrawPath(path, yellowPaint);
        }

        // Black outline
        using (var outlinePaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true })
        {
            var frontRect = new SKRect(16, 8, 40, 48);
            canvas.DrawRect(frontRect, outlinePaint);

            var path = new SKPath();
            path.MoveTo(40, 8);
            path.LineTo(48, 12);
            path.LineTo(48, 52);
            path.LineTo(24, 52);
            path.LineTo(16, 48);
            canvas.DrawPath(path, outlinePaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
