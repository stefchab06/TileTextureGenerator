using System.Reflection;
using SkiaSharp;
using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Tests.Common;

/// <summary>
/// Factory for creating valid test images in memory.
/// Generates real PNG images using SkiaSharp (no embedded resources needed).
/// All images are valid PNGs that can be used with ImageData without throwing exceptions.
/// </summary>
public static class TestImageFactory
{
    /// <summary>
    /// Creates a minimal 1x1 red PNG image (smallest valid PNG).
    /// Use this for tests that don't care about image content.
    /// </summary>
    /// <returns>Valid PNG image bytes.</returns>
    public static byte[] CreateValidPng()
    {
        using var bitmap = new SKBitmap(1, 1);
        bitmap.SetPixel(0, 0, SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Creates a PNG image with specified dimensions and color.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="color">Fill color.</param>
    /// <returns>Valid PNG image bytes.</returns>
    public static byte[] CreatePng(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Creates a PNG image with specified dimensions (blue color).
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>Valid PNG image bytes.</returns>
    public static byte[] CreatePng(int width, int height)
    {
        return CreatePng(width, height, SKColors.Blue);
    }

    /// <summary>
    /// Creates a valid ImageData with default 1x1 red PNG.
    /// Use this for tests that need a valid ImageData but don't care about content.
    /// </summary>
    /// <returns>Valid ImageData instance.</returns>
    public static ImageData CreateImageData()
    {
        return new ImageData(CreateValidPng());
    }

    /// <summary>
    /// Creates an ImageData with specified dimensions and color.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="color">Fill color.</param>
    /// <returns>Valid ImageData instance.</returns>
    public static ImageData CreateImageData(int width, int height, SKColor color)
    {
        return new ImageData(CreatePng(width, height, color));
    }

    /// <summary>
    /// Creates an ImageData with specified dimensions (blue color).
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>Valid ImageData instance.</returns>
    public static ImageData CreateImageData(int width, int height)
    {
        return new ImageData(CreatePng(width, height));
    }

    /// <summary>
    /// Creates a 256x256 PNG for DisplayImage tests (standard thumbnail size).
    /// Color: Blue
    /// </summary>
    /// <returns>Valid ImageData instance (256x256).</returns>
    public static ImageData CreateDisplayImage()
    {
        return CreateImageData(256, 256, SKColors.Blue);
    }

    /// <summary>
    /// Creates a 256x256 PNG with specified color for DisplayImage tests.
    /// </summary>
    /// <param name="color">Fill color.</param>
    /// <returns>Valid ImageData instance (256x256).</returns>
    public static ImageData CreateDisplayImage(SKColor color)
    {
        return CreateImageData(256, 256, color);
    }

    /// <summary>
    /// Load an image from resources.
    /// </summary>
    /// <param name="fielName">Resource image file name.</param>
    /// <returns>Valid ImageData instance.</returns>
    public static byte[] LoadTestImage(string filename)
    {

        var assembly = Assembly.GetExecutingAssembly();

        // Normalize filename (resources are case-sensitive in embedded names)
        // Map common names to actual resource names
        var normalizedName = filename switch
        {
            "valid.png" => "Valid.png",
            "landscape.jpg" => "LandScape.jpg",
            "portrait.jpg" => "Portrait.jpg",
            _ => filename
        };

        var resourceName = $"TileTextureGenerator.Tests.Common.Resources.TestImages.{normalizedName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Test image not found: {resourceName}. Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Load an ImageData from resources.
    /// </summary>
    /// <param name="fielName">Resource image file name.</param>
    /// <returns>Valid ImageData instance.</returns>
    public static ImageData CreateTestImageData(string filename)
    {
        return new ImageData(LoadTestImage(filename));
    }
}
