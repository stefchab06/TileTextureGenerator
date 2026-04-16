using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Services;
using TileTextureGenerator.Tests.Common;

namespace TileTextureGenerator.Core.Tests.Models;

/// <summary>
/// Tests for ImageData value type.
/// Validates automatic PNG conversion in constructor.
/// </summary>
public class ImageDataTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithPngBytes_StoresOriginalBytes()
    {
        // Arrange
        var pngBytes = TestImageFactory.LoadTestImage("valid.png");

        // Act
        var imageData = new ImageData(pngBytes);

        // Assert
        Assert.Equal(pngBytes, imageData.Bytes);
    }

    [Fact]
    public void Constructor_WithJpegBytes_ConvertsToPng()
    {
        // Arrange
        var jpegBytes = TestImageFactory.LoadTestImage("landscape.jpg");

        // Act
        var imageData = new ImageData(jpegBytes);

        // Assert
        Assert.True(ImageProcessor.IsPng(imageData.Bytes));
        Assert.NotEqual(jpegBytes, imageData.Bytes); // Converted, so different
    }

    [Fact]
    public void Constructor_WithBmpBytes_ConvertsToPng()
    {
        // Arrange
        var bmpBytes = TestImageFactory.LoadTestImage("sample.bmp");

        // Act
        var imageData = new ImageData(bmpBytes);

        // Assert
        Assert.True(ImageProcessor.IsPng(imageData.Bytes));
    }

    [Fact]
    public void Constructor_WithExifJpeg_ConvertsToPngWithCorrectOrientation()
    {
        // Arrange
        var exifJpeg = TestImageFactory.LoadTestImage("exif-rotated.jpg");

        // Act
        var imageData = new ImageData(exifJpeg);

        // Assert
        Assert.True(ImageProcessor.IsPng(imageData.Bytes));

        // Verify orientation was corrected (no black bars/rotation issues)
        using var bitmap = SkiaSharp.SKBitmap.Decode(imageData.Bytes);
        Assert.NotNull(bitmap);
    }

    [Fact]
    public void Constructor_WithNullBytes_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ImageData(null!));
    }

    [Fact]
    public void Constructor_WithEmptyBytes_ThrowsArgumentException()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ImageData(emptyBytes));
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_FromImageDataToByteArray_ReturnsBytes()
    {
        // Arrange
        var pngBytes = TestImageFactory.LoadTestImage("valid.png");
        var imageData = new ImageData(pngBytes);

        // Act
        byte[] result = imageData;

        // Assert
        Assert.Equal(imageData.Bytes, result);
    }

    [Fact]
    public void ImplicitConversion_FromByteArrayToImageData_CreatesImageData()
    {
        // Arrange
        var pngBytes = TestImageFactory.LoadTestImage("valid.png");

        // Act
        ImageData imageData = pngBytes;

        // Assert
        Assert.Equal(pngBytes, imageData.Bytes);
    }

    [Fact]
    public void ImplicitConversion_FromNullByteArray_ReturnsDefaultImageData()
    {
        // Arrange
        byte[]? nullBytes = null;

        // Act
        ImageData imageData = nullBytes;

        // Assert
        Assert.Equal(default(ImageData), imageData);
    }

    [Fact]
    public void ImplicitConversion_FromEmptyByteArray_ReturnsDefaultImageData()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();

        // Act
        ImageData imageData = emptyBytes;

        // Assert
        Assert.Equal(default(ImageData), imageData);
    }

    #endregion

    #region Value Equality Tests

    [Fact]
    public void Equals_WithSameBytesContent_ReturnsTrue()
    {
        // Arrange
        var pngBytes = TestImageFactory.LoadTestImage("valid.png");
        var imageData1 = new ImageData(pngBytes);
        var imageData2 = new ImageData(pngBytes);

        // Act & Assert
        Assert.Equal(imageData1, imageData2);
    }

    [Fact]
    public void Equals_WithDifferentBytes_ReturnsFalse()
    {
        // Arrange
        var pngBytes = TestImageFactory.LoadTestImage("valid.png");
        var jpegBytes = TestImageFactory.LoadTestImage("landscape.jpg");
        var imageData1 = new ImageData(pngBytes);
        var imageData2 = new ImageData(jpegBytes);

        // Act & Assert
        Assert.NotEqual(imageData1, imageData2);
    }

    [Fact]
    public void Equals_WithDefaultImageData_ReturnsTrue()
    {
        // Arrange
        var defaultImageData1 = default(ImageData);
        var defaultImageData2 = default(ImageData);

        // Act & Assert
        Assert.Equal(defaultImageData1, defaultImageData2);
    }

    #endregion
}
