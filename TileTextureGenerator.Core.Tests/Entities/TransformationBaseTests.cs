using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.DTOs;
using TransformationBase = TileTextureGenerator.Core.Entities.TransformationBase;

namespace TileTextureGenerator.Core.Tests.Entities;

/// <summary>
/// Unit tests for TransformationBase abstract class.
/// Tests core transformation behavior: initialization, properties, execution, persistence.
/// </summary>
public class TransformationBaseTests
{
    private sealed class TestTransformation : TransformationBase, ITransformationMetadata
    {
        private readonly ImageData _resultImage;

        public TestTransformation(ITransformationStore store, byte[]? resultImage = null) 
            : base(store)
        {
            _resultImage = new ImageData(resultImage ?? new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        }

        public static string IconResourceName => "Test.png";

        protected override Task<ImageData> ExecuteAsync()
        {
            return Task.FromResult(_resultImage);
        }
    }

    private class FakeTransformationStore : ITransformationStore
    {
        public List<TransformationBase> SavedTransformations { get; } = [];

        public Task SaveAsync(TransformationBase transformation)
        {
            SavedTransformations.Add(transformation);
            return Task.CompletedTask;
        }
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

    [Fact]
    public void Initialize_WithValidId_SetsIdAndType()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new TestTransformation(store);
        var id = Guid.NewGuid();

        // Act
        transformation.Initialize(project, id);

        // Assert
        Assert.Equal(id, transformation.Id);
        Assert.Equal(nameof(TestTransformation), transformation.Type);
        Assert.Equal(project, transformation.ParentProject);
    }

    [Fact]
    public void Initialize_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new TestTransformation(store);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformation.Initialize(project, Guid.Empty));
    }

    [Fact]
    public void Initialize_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new TestTransformation(store);
        transformation.Initialize(project, Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transformation.Initialize(project, Guid.NewGuid()));
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
    public void IconResourceName_ReturnsConcreteImplementation()
    {
        // Arrange & Act
        var iconResourceName = TestTransformation.IconResourceName;

        // Assert
        Assert.NotNull(iconResourceName);
        Assert.Equal("Test.png", iconResourceName);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsGeneratedImage()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var expectedImage = new byte[] { 1, 2, 3, 4, 5 };
        var transformation = new TestTransformation(store, expectedImage);
        transformation.Initialize(project, Guid.NewGuid());

        // Act
        var result = await transformation.GenerateAsync();

        // Assert
        Assert.Equal(expectedImage, result);
    }

    [Fact]
    public async Task SaveChangesAsync_CallsStore()
    {
        // Arrange
        var store = new FakeTransformationStore();
        var project = new FakeProject();
        var transformation = new TestTransformation(store);
        transformation.Initialize(project, Guid.NewGuid());

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
