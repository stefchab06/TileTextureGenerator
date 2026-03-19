using SkiaSharp;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteTransformations;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TransformationBase = TileTextureGenerator.Core.Entities.TransformationBase;

namespace TileTextureGenerator.Core.Tests.Entities.ConcreteTransformations;

/// <summary>
/// Unit tests for HorizontalFloorTransformation concrete transformation.
/// Tests execution, dimension calculations, and edge flap rendering.
/// </summary>
public class HorizontalFloorTransformationTests
{
    private class FakeTransformationStore : ITransformationStore
    {
        public Task SaveAsync(TransformationBase transformation) => Task.CompletedTask;
    }

    private sealed class FakeProject : ProjectBase
    {
        public FakeProject() : base(new FakeProjectStore())
        {
            Initialize("FakeProject");
        }

        public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
        {
            return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>(Array.Empty<TransformationTypeDTO>());
        }
    }

    private class FakeProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase entity) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationID) => Task.CompletedTask;
        public Task<TransformationBase> LoadTransformationAsync(ProjectBase project, Guid transformationId) => Task.FromResult<TransformationBase>(null!);
    }

    private byte[] CreateTestImage(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        
        // Draw a simple pattern for visual verification
        using var paint = new SKPaint { Color = SKColors.Blue };
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
        var project = new FakeProject();
        var transformation = new HorizontalFloorTransformation(store)
        {
            BaseTexture = CreateTestImage(400, 400), // 2" x 2" at 200 DPI
            TileShape = TileShape.Full
        };
        transformation.Initialize(project, Guid.NewGuid());

        // Act
        var result = await transformation.ExecuteAsync();

        // Assert
        Assert.NotEmpty(result.Bytes);

        // Verify dimensions: original + 2 * flap on each side
        // Flap = 0.25" * 200 DPI = 50 pixels
        // Expected: 400 + 2*50 = 500x500
        using var stream = new MemoryStream(result.Bytes);
        using var bitmap = SKBitmap.Decode(stream);
        
        Assert.NotNull(bitmap);
        Assert.Equal(500, bitmap.Width);
        Assert.Equal(500, bitmap.Height);
    }

    [Fact]
    public async Task ExecuteAsync_WithHalfHorizontalTile_ReturnsCorrectDimensions()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new HorizontalFloorTransformation(store)
        {
            BaseTexture = CreateTestImage(200, 400), // 1" x 2" at 200 DPI
            TileShape = TileShape.HalfHorizontal
        };
        transformation.Initialize(project, Guid.NewGuid());

        // Act
        var result = await transformation.ExecuteAsync();

        // Assert - Flap is based on max dimension (400px -> 2" -> 200 DPI -> 50px flap)
        using var stream = new MemoryStream(result.Bytes);
        using var bitmap = SKBitmap.Decode(stream);
        
        Assert.NotNull(bitmap);
        Assert.Equal(300, bitmap.Width);  // 200 + 2*50
        Assert.Equal(500, bitmap.Height); // 400 + 2*50
    }

    [Fact]
    public async Task ExecuteAsync_WithHalfVerticalTile_ReturnsCorrectDimensions()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new HorizontalFloorTransformation(store)
        {
            BaseTexture = CreateTestImage(400, 200), // 2" x 1" at 200 DPI
            TileShape = TileShape.HalfVertical
        };
        transformation.Initialize(project, Guid.NewGuid());

        // Act
        var result = await transformation.ExecuteAsync();

        // Assert
        using var stream = new MemoryStream(result.Bytes);
        using var bitmap = SKBitmap.Decode(stream);
        
        Assert.NotNull(bitmap);
        Assert.Equal(500, bitmap.Width);  // 400 + 2*50
        Assert.Equal(300, bitmap.Height); // 200 + 2*50
    }

    [Fact]
    public async Task ExecuteAsync_WithoutBaseTexture_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new HorizontalFloorTransformation(store);
        transformation.Initialize(project, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => transformation.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidImage_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new HorizontalFloorTransformation(store)
        {
            BaseTexture = new byte[] { 1, 2, 3, 4 } // Invalid PNG data
        };
        transformation.Initialize(project, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => transformation.ExecuteAsync());
    }

    [Theory]
    [InlineData(100, 100, 124, 124)]  // 100px (max=100 -> dpi=50 -> flap=12) -> 100+2*12=124
    [InlineData(200, 200, 250, 250)]  // 200px (max=200 -> dpi=100 -> flap=25) -> 200+2*25=250
    [InlineData(400, 400, 500, 500)]  // 400px (max=400 -> dpi=200 -> flap=50) -> 400+2*50=500
    [InlineData(800, 800, 1000, 1000)] // 800px (max=800 -> dpi=400 -> flap=100) -> 800+2*100=1000
    public async Task ExecuteAsync_CalculatesFlapSizeCorrectly(
        int baseWidth,
        int baseHeight,
        int expectedWidth,
        int expectedHeight)
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new HorizontalFloorTransformation(store)
        {
            BaseTexture = CreateTestImage(baseWidth, baseHeight)
        };
        transformation.Initialize(project, Guid.NewGuid());

        // Act
        var result = await transformation.ExecuteAsync();

        // Assert
        using var stream = new MemoryStream(result.Bytes);
        using var bitmap = SKBitmap.Decode(stream);
        
        Assert.NotNull(bitmap);
        Assert.Equal(expectedWidth, bitmap.Width);
        Assert.Equal(expectedHeight, bitmap.Height);
    }

    [Fact]
    public void RequiredPaperType_ReturnsStandard()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new HorizontalFloorTransformation(store);

        // Act
        var paperType = transformation.RequiredPaperType;

        // Assert
        Assert.Equal(PaperType.Standard, paperType);
    }
}
