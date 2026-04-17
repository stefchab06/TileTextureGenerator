using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using SkiaSharp;
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
    private byte[]? _imageBytes; // Store original bytes for SkiaSharp export
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
        _imageBytes = imageBytes; // Store for SkiaSharp export

        if (imageBytes != null && imageBytes.Length > 0)
        {
            try
            {
                // Load image using platform service with EXIF orientation correction
                using var stream = new MemoryStream(imageBytes);

#if WINDOWS
                _image = PlatformImage.FromStream(stream);
#else
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
    /// Uses SkiaSharp for off-screen rendering (more reliable than Microsoft.Maui.Graphics).
    /// </summary>
    public byte[]? GetCroppedImage()
    {
        if (_image == null || _croppingPolygon == null)
            return null;

        try
        {
            // Define a reference size for polygon conversion (e.g., 2000x2000 for high quality)
            const int referenceSize = 2000;

            // Convert proportional polygon (0-1) to pixel coordinates
            var polygonInPixels = _croppingPolygon
                .Select(p => new Point(p.X * referenceSize, p.Y * referenceSize))
                .ToList();

            // Calculate bounding box in pixels
            double minX = polygonInPixels.Min(p => p.X);
            double minY = polygonInPixels.Min(p => p.Y);
            double maxX = polygonInPixels.Max(p => p.X);
            double maxY = polygonInPixels.Max(p => p.Y);

            double polygonWidth = maxX - minX;
            double polygonHeight = maxY - minY;

            // Output size is the bounding box size
            int outputWidth = (int)Math.Ceiling(polygonWidth);
            int outputHeight = (int)Math.Ceiling(polygonHeight);

            // Ensure minimum size
            if (outputWidth < 10 || outputHeight < 10)
                return null;

            // Convert polygon to output coordinates (relative to bounding box origin)
            var polygonInOutputPixels = polygonInPixels
                .Select(p => new SKPoint(
                    (float)(p.X - minX),
                    (float)(p.Y - minY)
                ))
                .ToArray();

            // Calculate fit-to-fill for the output size
            var imageSize = new SizeF(_image.Width, _image.Height);
            var outputSize = new SizeF(outputWidth, outputHeight);
            var polygonInOutputPoints = polygonInOutputPixels.Select(p => new Point(p.X, p.Y)).ToList();

            var fitToFill = CroppingMath.CalculateFitToFill(imageSize, outputSize, polygonInOutputPoints);

            // Create SkiaSharp bitmap for off-screen rendering
            using var surface = SKSurface.Create(new SKImageInfo(outputWidth, outputHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;

            // Clear with transparent background
            canvas.Clear(SKColors.Transparent);

            // Create clipping path
            using var clipPath = new SKPath();
            clipPath.MoveTo(polygonInOutputPixels[0]);
            for (int i = 1; i < polygonInOutputPixels.Length; i++)
            {
                clipPath.LineTo(polygonInOutputPixels[i]);
            }
            clipPath.Close();

            // Apply clipping
            canvas.Save();
            canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);

            // Apply transformations
            canvas.Translate(fitToFill.Offset.X, fitToFill.Offset.Y);
            canvas.Scale((float)fitToFill.Zoom, (float)fitToFill.Zoom);
            canvas.Translate((float)_transformation.PanX, (float)_transformation.PanY);

            // Apply zoom transformation if any
            if (_transformation.Zoom != 1.0)
            {
                float centerX = _image.Width / 2f;
                float centerY = _image.Height / 2f;
                canvas.Translate(centerX, centerY);
                canvas.Scale((float)_transformation.Zoom, (float)_transformation.Zoom);
                canvas.Translate(-centerX, -centerY);
            }

            // Apply rotation if any
            if (_transformation.Rotation != 0)
            {
                float centerX = _image.Width / 2f;
                float centerY = _image.Height / 2f;
                canvas.Translate(centerX, centerY);
                canvas.RotateDegrees((float)_transformation.Rotation);
                canvas.Translate(-centerX, -centerY);
            }

            // Convert Microsoft.Maui.Graphics.IImage to SKBitmap
            // We reload from the original bytes with EXIF orientation correction
            using var memoryStream = new SKMemoryStream(_imageBytes);
            using var codec = SKCodec.Create(memoryStream);

            if (codec == null)
                return null;

            using var originalBitmap = SKBitmap.Decode(codec);
            if (originalBitmap == null)
                return null;

            // Apply EXIF orientation correction (for JPEG from cameras/phones)
            var origin = codec.EncodedOrigin;
            using var tempBitmap = ApplyExifOrientation(originalBitmap, origin);

            canvas.DrawBitmap(tempBitmap, 0, 0);

            canvas.Restore();

            // Encode to PNG
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to crop image: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Applies EXIF orientation transformation to a bitmap.
    /// Handles rotation and flipping based on camera orientation metadata.
    /// Copied from TileTextureGenerator.Core.Services.ImageProcessor (architectural isolation).
    /// </summary>
    private static SKBitmap ApplyExifOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        // If no rotation needed, return original
        if (origin == SKEncodedOrigin.TopLeft)
            return bitmap;

        // Calculate new dimensions and transformations needed
        var needsSwapDimensions = origin == SKEncodedOrigin.LeftTop ||
                                   origin == SKEncodedOrigin.RightTop ||
                                   origin == SKEncodedOrigin.RightBottom ||
                                   origin == SKEncodedOrigin.LeftBottom;

        var width = needsSwapDimensions ? bitmap.Height : bitmap.Width;
        var height = needsSwapDimensions ? bitmap.Width : bitmap.Height;

        // Create new bitmap with correct dimensions
        var result = new SKBitmap(width, height, bitmap.ColorType, bitmap.AlphaType);

        using (var canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Transparent);

            // Apply transformations based on EXIF orientation
            // Reference: EXIF Orientation Tag values (1-8)
            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    // Flip horizontal
                    canvas.Scale(-1, 1, width / 2f, 0);
                    break;

                case SKEncodedOrigin.BottomRight:
                    // Rotate 180°
                    canvas.RotateDegrees(180, width / 2f, height / 2f);
                    break;

                case SKEncodedOrigin.BottomLeft:
                    // Flip vertical
                    canvas.Scale(1, -1, 0, height / 2f);
                    break;

                case SKEncodedOrigin.LeftTop:
                    // Rotate 90° CCW + flip horizontal = Transpose
                    canvas.Translate(0, height);
                    canvas.RotateDegrees(-90);
                    canvas.Scale(-1, 1, bitmap.Height / 2f, 0);
                    break;

                case SKEncodedOrigin.RightTop:
                    // Rotate 90° CW (most common for phones in portrait)
                    canvas.Translate(width, 0);
                    canvas.RotateDegrees(90);
                    break;

                case SKEncodedOrigin.RightBottom:
                    // Rotate 90° CW + flip horizontal = Transverse
                    canvas.Translate(width, 0);
                    canvas.RotateDegrees(90);
                    canvas.Scale(-1, 1, height / 2f, 0);
                    break;

                case SKEncodedOrigin.LeftBottom:
                    // Rotate 90° CCW
                    canvas.Translate(0, height);
                    canvas.RotateDegrees(-90);
                    break;
            }

            canvas.DrawBitmap(bitmap, 0, 0);
        }

        return result;
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
