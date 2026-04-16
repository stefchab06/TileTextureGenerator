using SkiaSharp;
using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.Services;

/// <summary>
/// Static image processing service for Core domain.
/// Handles PNG conversion and resizing using SkiaSharp.
/// All images in Core domain MUST be PNG format.
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// Checks if the image data is in PNG format.
    /// Validates PNG signature (first 8 bytes).
    /// </summary>
    /// <param name="bytes">Raw image bytes.</param>
    /// <returns>True if PNG format, false otherwise.</returns>
    public static bool IsPng(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 8)
            return false;

        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        return bytes[0] == 0x89 &&
               bytes[1] == 0x50 &&
               bytes[2] == 0x4E &&
               bytes[3] == 0x47 &&
               bytes[4] == 0x0D &&
               bytes[5] == 0x0A &&
               bytes[6] == 0x1A &&
               bytes[7] == 0x0A;
    }

    /// <summary>
    /// Converts any image format to PNG.
    /// If already PNG, returns unchanged.
    /// Supports JPEG, BMP, GIF, WebP, and other formats supported by SkiaSharp.
    /// Automatically corrects orientation based on EXIF metadata (for JPEG).
    /// </summary>
    /// <param name="bytes">Raw image bytes (any format).</param>
    /// <returns>PNG image bytes with correct orientation.</returns>
    /// <exception cref="ArgumentException">If bytes cannot be decoded as a valid image.</exception>
    public static byte[] ConvertToPng(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length == 0)
            throw new ArgumentException("Image data cannot be empty.", nameof(bytes));

        // If already PNG, return unchanged
        if (IsPng(bytes))
            return bytes;

        // Decode image using SkiaSharp with orientation correction
        using var stream = new SKMemoryStream(bytes);
        using var codec = SKCodec.Create(stream);

        if (codec == null)
            throw new ArgumentException("Unable to decode image. Format may be unsupported or data corrupted.", nameof(bytes));

        // Get image info and decode
        var info = codec.Info;
        using var bitmap = SKBitmap.Decode(codec);

        if (bitmap == null)
            throw new ArgumentException("Unable to decode image bitmap.", nameof(bytes));

        // Apply EXIF orientation correction (for JPEG from cameras/phones)
        var origin = codec.EncodedOrigin;
        using var correctedBitmap = ApplyExifOrientation(bitmap, origin);

        // Encode to PNG
        using var image = SKImage.FromBitmap(correctedBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    /// <summary>
    /// Applies EXIF orientation transformation to a bitmap.
    /// Handles rotation and flipping based on camera orientation metadata.
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
    /// Resizes a PNG image to specified dimensions.
    /// Converts to PNG first if needed.
    /// Maintains aspect ratio by fitting within the target dimensions.
    /// </summary>
    /// <param name="image">Source image (will be converted to PNG if needed).</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <returns>Resized PNG image.</returns>
    /// <exception cref="ArgumentException">If width/height are invalid or image cannot be decoded.</exception>
    public static ImageData ResizeImage(ImageData image, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive.");

        // Ensure PNG format first
        var pngBytes = ConvertToPng(image.Bytes);

        // Decode image
        using var originalBitmap = SKBitmap.Decode(pngBytes);

        if (originalBitmap == null)
            throw new ArgumentException("Unable to decode image for resizing.", nameof(image));

        // Create resized bitmap
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var resizedBitmap = originalBitmap.Resize(info, SKFilterQuality.High);

        if (resizedBitmap == null)
            throw new InvalidOperationException($"Failed to resize image to {width}x{height}.");

        // Encode to PNG
        using var resizedImage = SKImage.FromBitmap(resizedBitmap);
        using var data = resizedImage.Encode(SKEncodedImageFormat.Png, 100);

        return new ImageData(data.ToArray());
    }
}
