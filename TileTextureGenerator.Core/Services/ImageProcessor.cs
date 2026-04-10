using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.Services;

/// <summary>
/// Static image processing service for Core domain.
/// Handles PNG conversion and resizing.
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
    /// </summary>
    /// <param name="bytes">Raw image bytes (any format).</param>
    /// <returns>PNG image bytes.</returns>
    public static byte[] ConvertToPng(byte[] bytes)
    {
        if (IsPng(bytes))
            return bytes;

        // TODO: Implement actual conversion (using SkiaSharp, ImageSharp, or System.Drawing)
        // For now, throw if not PNG (will be implemented in Infrastructure layer)
        throw new NotImplementedException(
            "Image conversion not yet implemented. " +
            "Please provide PNG images only, or implement conversion using an image library.");
    }

    /// <summary>
    /// Resizes a PNG image to specified dimensions.
    /// Converts to PNG first if needed.
    /// </summary>
    /// <param name="image">Source image (will be converted to PNG if needed).</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <returns>Resized PNG image.</returns>
    public static ImageData ResizeImage(ImageData image, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive.");

        var pngBytes = ConvertToPng(image.Bytes);

        // TODO: Implement actual resizing (using SkiaSharp, ImageSharp, or System.Drawing)
        // For now, return unchanged (will be implemented in Infrastructure layer)
        throw new NotImplementedException(
            "Image resizing not yet implemented. " +
            "Implement using an image library (SkiaSharp recommended for MAUI).");
    }
}
