using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace TileTextureGenerator.Presentation.UI.Controls.ImageCropping;

/// <summary>
/// GraphicsView-based control for displaying an image with a cropping polygon overlay.
/// Supports transformation (pan, zoom, rotate) and visual feedback.
/// Uses Microsoft.Maui.Graphics (100% compatible with .NET MAUI).
/// </summary>
public class CroppingCanvasControl : GraphicsView
{
    private IImage? _image;
    private IReadOnlyList<Point>? _croppingPolygon;
    private CroppingTransformation _transformation = CroppingTransformation.Identity;
    private (double Zoom, PointF Offset) _initialFit;

    public CroppingCanvasControl()
    {
        Drawable = new CroppingDrawable(this);
        BackgroundColor = Colors.LightGray;
    }

    /// <summary>
    /// Sets the image to display (from byte array).
    /// Calculates initial fit-to-fill transformation.
    /// </summary>
    public void SetImage(byte[]? imageBytes, IReadOnlyList<Point>? croppingPolygon)
    {
        _croppingPolygon = croppingPolygon;

        if (imageBytes != null && imageBytes.Length > 0)
        {
            try
            {
                // Load image using platform service
                using var stream = new MemoryStream(imageBytes);

#if WINDOWS
                _image = PlatformImage.FromStream(stream);
#else
                // For other platforms, use the platform-specific loader
                _image = PlatformImage.FromStream(stream);
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
                _image = null;
            }
        }
        else
        {
            _image = null;
        }

        // Initial fit will be calculated in Draw() when we know the canvas size
        _initialFit = (1.0, new PointF(0, 0));

        Invalidate(); // Trigger repaint
    }

    /// <summary>
    /// Updates the transformation (pan, zoom, rotate).
    /// </summary>
    public void SetTransformation(CroppingTransformation transformation)
    {
        _transformation = transformation;
        Invalidate();
    }

    /// <summary>
    /// Gets the final cropped image with polygon mask applied.
    /// </summary>
    public byte[]? GetCroppedImage()
    {
        // TODO: Apply polygon mask and return PNG with transparency
        // For now, return the original image
        if (_image == null)
            return null;

        // This will be implemented in Phase 2
        return null;
    }

    /// <summary>
    /// Internal drawable class that handles the actual rendering.
    /// </summary>
    private class CroppingDrawable : IDrawable
    {
        private readonly CroppingCanvasControl _control;

        public CroppingDrawable(CroppingCanvasControl control)
        {
            _control = control;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Clear background
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            if (_control._image == null || _control._croppingPolygon == null)
            {
                // Draw placeholder text
                DrawPlaceholder(canvas, dirtyRect);
                return;
            }

            // Draw the image with transformations
            DrawTransformedImage(canvas, dirtyRect);

            // Draw cropping polygon overlay
            DrawCroppingPolygon(canvas, dirtyRect);
        }

        private void DrawPlaceholder(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 24;
            canvas.DrawString(
                "GraphicsView Canvas\nClick 📁 to load image",
                dirtyRect.Center.X,
                dirtyRect.Center.Y,
                HorizontalAlignment.Center
            );
        }

        private void DrawTransformedImage(ICanvas canvas, RectF dirtyRect)
        {
            if (_control._image == null || _control._croppingPolygon == null)
                return;

            // Convert proportional polygon (0-1) to canvas pixels with proper aspect ratio
            var polygonInPixels = ConvertPolygonToPixels(_control._croppingPolygon, dirtyRect);

            // Calculate fit-to-fill with the pixel-based polygon
            var imageSize = new SizeF(_control._image.Width, _control._image.Height);
            var initialFit = CroppingMath.CalculateFitToFill(imageSize, dirtyRect.Size, polygonInPixels);

            canvas.SaveState();

            try
            {
                // Apply initial fit-to-fill transformation
                canvas.Translate(initialFit.Offset.X, initialFit.Offset.Y);
                canvas.Scale((float)initialFit.Zoom, (float)initialFit.Zoom);

                // Apply user transformations (pan only for now)
                canvas.Translate((float)_control._transformation.PanX, (float)_control._transformation.PanY);

                // TODO: Apply zoom and rotation transformations (Phase 2)

                // Draw the image
                canvas.DrawImage(_control._image, 0, 0, _control._image.Width, _control._image.Height);
            }
            finally
            {
                canvas.RestoreState();
            }
        }

        private void DrawCroppingPolygon(ICanvas canvas, RectF dirtyRect)
        {
            if (_control._croppingPolygon == null || _control._croppingPolygon.Count < 3)
                return;

            // Convert proportional polygon (0-1) to canvas pixels with proper aspect ratio
            var polygonInPixels = ConvertPolygonToPixels(_control._croppingPolygon, dirtyRect);

            var path = new PathF();
            path.MoveTo((float)polygonInPixels[0].X, (float)polygonInPixels[0].Y);

            for (int i = 1; i < polygonInPixels.Count; i++)
            {
                path.LineTo((float)polygonInPixels[i].X, (float)polygonInPixels[i].Y);
            }

            path.Close();

            // Draw polygon border (red outline)
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 3;
            canvas.DrawPath(path);
        }

        /// <summary>
        /// Converts a proportional polygon (0-1 coordinates) to canvas pixels,
        /// preserving aspect ratio and applying a margin.
        /// </summary>
        private List<Point> ConvertPolygonToPixels(IReadOnlyList<Point> polygon, RectF canvasRect)
        {
            // Apply 10% margin on each side (80% of canvas for the polygon)
            const double marginPercent = 0.1;
            float availableWidth = canvasRect.Width * (1 - 2 * (float)marginPercent);
            float availableHeight = canvasRect.Height * (1 - 2 * (float)marginPercent);

            // Calculate bounding box of the proportional polygon (0-1)
            double minX = polygon.Min(p => p.X);
            double minY = polygon.Min(p => p.Y);
            double maxX = polygon.Max(p => p.X);
            double maxY = polygon.Max(p => p.Y);

            double polygonWidth = maxX - minX;
            double polygonHeight = maxY - minY;

            // Calculate scale to fit in available space while preserving aspect ratio
            double scaleX = availableWidth / polygonWidth;
            double scaleY = availableHeight / polygonHeight;
            double scale = Math.Min(scaleX, scaleY); // Use minimum to fit entirely

            // Calculate scaled polygon size
            double scaledWidth = polygonWidth * scale;
            double scaledHeight = polygonHeight * scale;

            // Center the polygon in the canvas (with margins)
            float marginLeft = canvasRect.Width * (float)marginPercent;
            float marginTop = canvasRect.Height * (float)marginPercent;

            double offsetX = marginLeft + (availableWidth - scaledWidth) / 2.0;
            double offsetY = marginTop + (availableHeight - scaledHeight) / 2.0;

            // Convert each point
            return polygon.Select(p => new Point(
                (p.X - minX) * scale + offsetX,
                (p.Y - minY) * scale + offsetY
            )).ToList();
        }
    }
}
