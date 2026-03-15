using SkiaSharp;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteTransformations;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TransformationBase = TileTextureGenerator.Core.Entities.TransformationBase;

namespace TileTextureGenerator.Core.Tests.Entities.ConcreteTransformations;

/// <summary>
/// Unit tests for VerticalWallTransformation concrete transformation.
/// Tests execution, dimension calculations, and edge flap rendering.
/// </summary>
public class VerticalWallTransformationTests
{
    private class FakeTransformationStore : ITransformationStore<TransformationBase>
    {
        public Task SaveAsync(TransformationBase transformation) => Task.CompletedTask;
        public Task<TransformationBase?> LoadAsync(Guid transformationId) => Task.FromResult<TransformationBase?>(null);
    }

    private byte[] CreateTestImage(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        
        // Draw a simple pattern for visual verification
        using var paint = new SKPaint { Color = SKColors.Red };
        canvas.DrawRect(10, 10, width - 20, height - 20, paint);
        
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    [Fact]
    public async Task ExecuteAsync_WithFullTile_ReturnsCorrectDimensions()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new VerticalWallTransformation(store)
        {
            BaseTexture = CreateTestImage(400, 400), // 2" x 2" at 200 DPI
            TileShape = TileShape.Full
        };
        transformation.Initialize(Guid.NewGuid());

        // Act
        var result = await transformation.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify dimensions: original + 2 * flap on each side
        // Flap = 0.25" * 200 DPI = 50 pixels
        // Expected: 400 + 2*50 = 500x500
        using var stream = new MemoryStream(result);
        using var bitmap = SKBitmap.Decode(stream);
        
        Assert.NotNull(bitmap);
        Assert.Equal(500, bitmap.Width);
        Assert.Equal(500, bitmap.Height);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutBaseTexture_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new VerticalWallTransformation(store);
        transformation.Initialize(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => transformation.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidImage_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new VerticalWallTransformation(store)
        {
            BaseTexture = new byte[] { 1, 2, 3, 4 } // Invalid PNG data
        };
        transformation.Initialize(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => transformation.ExecuteAsync());
    }
}
