using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TransformationBase = TileTextureGenerator.Core.Entities.TransformationBase;

namespace TileTextureGenerator.Core.Tests.Entities;

/// <summary>
/// Unit tests for TransformationBase abstract class.
/// Tests core transformation behavior: initialization, properties, execution, persistence.
/// </summary>
public class TransformationBaseTests
{
    private sealed class TestTransformation : TransformationBase
    {
        private readonly ImageData _resultImage;

        public TestTransformation(ITransformationStore<TransformationBase> store, byte[]? resultImage = null) 
            : base(store)
        {
            _resultImage = new ImageData(resultImage ?? new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        }

        public override ImageData? Icon => new ImageData(new byte[] { 0x49, 0x43, 0x4F, 0x4E }); // "ICON" mock

        public override Task<ImageData> ExecuteAsync()
        {
            return Task.FromResult(_resultImage);
        }
    }

    private class FakeTransformationStore : ITransformationStore<TransformationBase>
    {
        public List<TransformationBase> SavedTransformations { get; } = [];

        public Task SaveAsync(TransformationBase transformation)
        {
            SavedTransformations.Add(transformation);
            return Task.CompletedTask;
        }

        public Task<TransformationBase?> LoadAsync(Guid transformationId) 
            => Task.FromResult<TransformationBase?>(null);
    }

    [Fact]
    public void Initialize_WithValidId_SetsIdAndType()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);
        var id = Guid.NewGuid();

        // Act
        transformation.Initialize(id);

        // Assert
        Assert.Equal(id, transformation.Id);
        Assert.Equal(nameof(TestTransformation), transformation.Type);
    }

    [Fact]
    public void Initialize_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformation.Initialize(Guid.Empty));
    }

    [Fact]
    public void Initialize_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);
        transformation.Initialize(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transformation.Initialize(Guid.NewGuid()));
    }

    [Fact]
    public void Id_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transformation.Id);
    }

    [Fact]
    public void Type_BeforeInitialize_ReturnsEmptyString()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act & Assert
        Assert.Equal(string.Empty, transformation.Type);
    }

    [Fact]
    public void Icon_ReturnsConcreteImplementation()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act
        var icon = transformation.Icon;

        // Assert
        Assert.NotNull(icon);
        Assert.Equal(new byte[] { 0x49, 0x43, 0x4F, 0x4E }, icon);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsGeneratedImage()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var expectedImage = new byte[] { 1, 2, 3, 4, 5 };
        var transformation = new TestTransformation(store, expectedImage);
        transformation.Initialize(Guid.NewGuid());

        // Act
        var result = await transformation.ExecuteAsync();

        // Assert
        Assert.Equal(expectedImage, result);
    }

    [Fact]
    public async Task SaveChangesAsync_CallsStore()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);
        transformation.Initialize(Guid.NewGuid());

        // Act
        await transformation.SaveChangesAsync();

        // Assert
        Assert.Single(store.SavedTransformations);
        Assert.Contains(transformation, store.SavedTransformations);
    }

    [Fact]
    public async Task SaveChangesAsync_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => transformation.SaveChangesAsync());
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestTransformation(null!));
    }

    [Fact]
    public void Constructor_InitializesEdgeFlapsWithDefaults()
    {
        // Arrange
        var store = new FakeTransformationStore();

        // Act
        var transformation = new TestTransformation(store);

        // Assert
        Assert.NotNull(transformation.EdgeFlap[ImageSide.Top]);
        Assert.NotNull(transformation.EdgeFlap[ImageSide.Right]);
        Assert.NotNull(transformation.EdgeFlap[ImageSide.Bottom]);
        Assert.NotNull(transformation.EdgeFlap[ImageSide.Left]);
        Assert.Equal(EdgeFlapMode.Blank, transformation.EdgeFlap[ImageSide.Top].Mode);
        Assert.Equal(EdgeFlapMode.Blank, transformation.EdgeFlap[ImageSide.Right].Mode);
        Assert.Equal(EdgeFlapMode.Blank, transformation.EdgeFlap[ImageSide.Bottom].Mode);
        Assert.Equal(EdgeFlapMode.Blank, transformation.EdgeFlap[ImageSide.Left].Mode);
    }

    [Fact]
    public void EdgeFlaps_CanBeModified()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act
        transformation.EdgeFlap[ImageSide.Top] = new EdgeFlapConfiguration { Mode = EdgeFlapMode.Color, Color = "#FF0000" };
        transformation.EdgeFlap[ImageSide.Right] = new EdgeFlapConfiguration { Mode = EdgeFlapMode.Texture, Texture = new ImageData(new byte[] { 0, 1, 2 }) };

        // Assert
        Assert.Equal(EdgeFlapMode.Color, transformation.EdgeFlap[ImageSide.Top].Mode);
        Assert.Equal("#FF0000", transformation.EdgeFlap[ImageSide.Top].Color);
        Assert.Equal(EdgeFlapMode.Texture, transformation.EdgeFlap[ImageSide.Right].Mode);
        Assert.Equal(new byte[] { 0, 1, 2 }, transformation.EdgeFlap[ImageSide.Right].Texture!.Value.Bytes);
    }

    [Fact]
    public void EdgeFlaps_SetNull_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => transformation.EdgeFlap[ImageSide.Top] = null!);
    }

    [Fact]
    public void RequiredPaperType_DefaultsToStandard()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var transformation = new TestTransformation(store);

        // Act
        var paperType = transformation.RequiredPaperType;

        // Assert
        Assert.Equal(PaperType.Standard, paperType);
    }
}
