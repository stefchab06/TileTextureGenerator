using SkiaSharp;
using TileTextureGenerator.Core.DTOs;
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
[Collection("TransformationRegistry")]
public class VerticalWallTransformationTests
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
        public Task ArchiveAsync(ProjectBase project) => Task.CompletedTask;
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
    public async Task GenerateAsync_WithFullTile_ReturnsCorrectDimensions()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new VerticalWallTransformation(store)
        {
            BaseTexture = CreateTestImage(400, 400), // 2" x 2" at 200 DPI
            TileShape = TileShape.Full
        };
        transformation.Initialize(project, Guid.NewGuid());

        // Act
        var result = await transformation.GenerateAsync();

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
    public async Task GenerateAsync_WithoutBaseTexture_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new VerticalWallTransformation(store);
        transformation.Initialize(project, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => transformation.GenerateAsync());
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidImage_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new VerticalWallTransformation(store)
        {
            BaseTexture = new byte[] { 1, 2, 3, 4 } // Invalid PNG data
        };
        transformation.Initialize(project, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => transformation.GenerateAsync());
    }

    [Fact]
    public void RequiredPaperType_ReturnsStandard()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new VerticalWallTransformation(store);

        // Act
        var paperType = transformation.RequiredPaperType;

        // Assert
        Assert.Equal(PaperType.Standard, paperType);
    }
}
