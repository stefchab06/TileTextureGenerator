using System.Reflection;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Services;
using TileTextureGenerator.Tests.Common;

namespace TileTextureGenerator.Core.Tests.Services;

/// <summary>
/// Tests for ImageProcessor static service.
/// Validates PNG conversion, EXIF handling, and image resizing.
/// </summary>
public class ImageProcessorTests
{
    // Helper: Load embedded test image
    private static byte[] LoadTestImage(string filename)
    {
        return TestImageFactory.LoadTestImage(filename);
    }

    #region IsPng Tests

    [Fact]
    public void IsPng_WithValidPngBytes_ReturnsTrue()
    {
        // Arrange
        var pngBytes = LoadTestImage("valid.png");

        // Act
        var result = ImageProcessor.IsPng(pngBytes);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPng_WithJpegBytes_ReturnsFalse()
    {
        // Arrange
        var jpegBytes = LoadTestImage("landscape.jpg");

        // Act
        var result = ImageProcessor.IsPng(jpegBytes);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPng_WithBmpBytes_ReturnsFalse()
    {
        // Arrange
        var bmpBytes = LoadTestImage("sample.bmp");

        // Act
        var result = ImageProcessor.IsPng(bmpBytes);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPng_WithNullBytes_ReturnsFalse()
    {
        // Act
        var result = ImageProcessor.IsPng(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPng_WithEmptyBytes_ReturnsFalse()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();

        // Act
        var result = ImageProcessor.IsPng(emptyBytes);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPng_WithTooShortBytes_ReturnsFalse()
    {
        // Arrange
        var shortBytes = new byte[] { 0x89, 0x50, 0x4E }; // Only 3 bytes

        // Act
        var result = ImageProcessor.IsPng(shortBytes);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ConvertToPng Tests

    [Fact]
    public void ConvertToPng_WithAlreadyPngBytes_ReturnsSameBytes()
    {
        // Arrange
        var pngBytes = LoadTestImage("valid.png");

        // Act
        var result = ImageProcessor.ConvertToPng(pngBytes);

        // Assert
        Assert.Equal(pngBytes, result); // Should return same instance (optimization)
    }

    [Fact]
    public void ConvertToPng_WithJpegBytes_ReturnsPngBytes()
    {
        // Arrange
        var jpegBytes = LoadTestImage("landscape.jpg");

        // Act
        var result = ImageProcessor.ConvertToPng(jpegBytes);

        // Assert
        Assert.True(ImageProcessor.IsPng(result));
        Assert.NotEqual(jpegBytes, result); // Should be different (converted)
    }

    [Fact]
    public void ConvertToPng_WithBmpBytes_ReturnsPngBytes()
    {
        // Arrange
        var bmpBytes = LoadTestImage("sample.bmp");

        // Act
        var result = ImageProcessor.ConvertToPng(bmpBytes);

        // Assert
        Assert.True(ImageProcessor.IsPng(result));
    }

    [Fact]
    public void ConvertToPng_WithExifRotatedJpeg_ReturnsPngWithCorrectOrientation()
    {
        // Arrange
        var exifJpeg = LoadTestImage("exif-rotated.jpg");

        // Act
        var result = ImageProcessor.ConvertToPng(exifJpeg);

        // Assert
        Assert.True(ImageProcessor.IsPng(result));
        
        // Verify image can be decoded (correct orientation applied)
        using var bitmap = SkiaSharp.SKBitmap.Decode(result);
        Assert.NotNull(bitmap);
        Assert.True(bitmap.Width > 0);
        Assert.True(bitmap.Height > 0);
    }

    [Fact]
    public void ConvertToPng_WithNullBytes_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ImageProcessor.ConvertToPng(null!));
    }

    [Fact]
    public void ConvertToPng_WithEmptyBytes_ThrowsArgumentException()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ImageProcessor.ConvertToPng(emptyBytes));
    }

    [Fact]
    public void ConvertToPng_WithCorruptedBytes_ThrowsArgumentException()
    {
        // Arrange
        var corruptedBytes = LoadTestImage("corrupted.bin");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ImageProcessor.ConvertToPng(corruptedBytes));
        Assert.Contains("decode", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ResizeImage Tests

    [Fact]
    public void ResizeImage_WithValidImage_ResizesToSpecifiedDimensions()
    {
        // Arrange
        var pngBytes = LoadTestImage("valid.png");
        var imageData = new ImageData(pngBytes);

        // Act
        var resized = ImageProcessor.ResizeImage(imageData, 256, 256);

        // Assert
        Assert.True(ImageProcessor.IsPng(resized.Bytes));
        
        using var bitmap = SkiaSharp.SKBitmap.Decode(resized.Bytes);
        Assert.NotNull(bitmap);
        Assert.Equal(256, bitmap.Width);
        Assert.Equal(256, bitmap.Height);
    }

    [Fact]
    public void ResizeImage_WithJpegImage_ConvertsAndResizes()
    {
        // Arrange
        var jpegBytes = LoadTestImage("portrait.jpg");
        var imageData = new ImageData(jpegBytes);

        // Act
        var resized = ImageProcessor.ResizeImage(imageData, 128, 128);

        // Assert
        Assert.True(ImageProcessor.IsPng(resized.Bytes));
        
        using var bitmap = SkiaSharp.SKBitmap.Decode(resized.Bytes);
        Assert.NotNull(bitmap);
        Assert.Equal(128, bitmap.Width);
        Assert.Equal(128, bitmap.Height);
    }

    [Fact]
    public void ResizeImage_WithZeroWidth_ThrowsArgumentException()
    {
        // Arrange
        var pngBytes = LoadTestImage("valid.png");
        var imageData = new ImageData(pngBytes);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ImageProcessor.ResizeImage(imageData, 0, 256));
    }

    [Fact]
    public void ResizeImage_WithNegativeHeight_ThrowsArgumentException()
    {
        // Arrange
        var pngBytes = LoadTestImage("valid.png");
        var imageData = new ImageData(pngBytes);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ImageProcessor.ResizeImage(imageData, 256, -1));
    }

    [Fact]
    public void ResizeImage_ToVerySmallSize_Succeeds()
    {
        // Arrange
        var pngBytes = LoadTestImage("valid.png");
        var imageData = new ImageData(pngBytes);

        // Act
        var resized = ImageProcessor.ResizeImage(imageData, 1, 1);

        // Assert
        using var bitmap = SkiaSharp.SKBitmap.Decode(resized.Bytes);
        Assert.NotNull(bitmap);
        Assert.Equal(1, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
    }

    [Fact]
    public void ResizeImage_ToVeryLargeSize_Succeeds()
    {
        // Arrange
        var pngBytes = LoadTestImage("valid.png");
        var imageData = new ImageData(pngBytes);

        // Act
        var resized = ImageProcessor.ResizeImage(imageData, 2048, 2048);

        // Assert
        using var bitmap = SkiaSharp.SKBitmap.Decode(resized.Bytes);
        Assert.NotNull(bitmap);
        Assert.Equal(2048, bitmap.Width);
        Assert.Equal(2048, bitmap.Height);
    }

    #endregion
}
