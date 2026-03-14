using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Core.Tests.Entities;

/// <summary>
/// Unit tests for ProjectBase abstract class.
/// Tests core project behavior: creation, properties, display image handling.
/// </summary>
public class ProjectBaseTests
{
    private sealed class TestProject : ProjectBase
    {
        public TestProject(IProjectStore<ProjectBase> store) : base(store)
        {
        }

        public override Task AddTransformationAsync(TransformationEntity transformation)
        {
            return Task.CompletedTask;
        }

        public override Task RemoveTransformationAsync(Guid transformationId)
        {
            return Task.CompletedTask;
        }

        public override Task ReorderTransformationsAsync(IReadOnlyList<Guid> newOrder)
        {
            return Task.CompletedTask;
        }
    }

    private class FakeProjectStore : IProjectStore<ProjectBase>
    {
        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
        public Task<ProjectBase?> LoadAsync(string projectName) => Task.FromResult<ProjectBase?>(null);
    }

    private class FakeImageProcessingService : IImageProcessingService
    {
        public byte[] ConvertToPng(byte[] sourceData)
        {
            return new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header mock
        }

        public byte[] ConvertToPng(byte[] sourceData, int width, int height)
        {
            return new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header mock
        }
    }

    [Fact]
    public void Constructor_WithValidName_SetsName()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);

        // Act
        project.Initialize("MyProject");

        // Assert
        Assert.Equal("MyProject", project.Name);
        Assert.Equal(nameof(TestProject), project.Type);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => project.Initialize(null!));
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => project.Initialize(""));
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => project.Initialize("   "));
    }

    [Fact]
    public void Constructor_SetsDefaultStatus()
    {
        // Arrange
        var store = new FakeProjectStore();

        // Act
        var project = new TestProject(store);
        project.Initialize("TestProject");

        // Assert
        Assert.Equal(ProjectStatus.Unexisting, project.Status);
    }

    [Fact]
    public void Constructor_SetsLastModifiedDateToNow()
    {
        // Arrange
        var store = new FakeProjectStore();
        var before = DateTime.UtcNow;

        // Act
        var project = new TestProject(store);
        project.Initialize("TestProject");

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(project.LastModifiedDate, before, after);
    }

    [Fact]
    public void Status_CanBeModified()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");

        // Act
        project.Status = ProjectStatus.Generated;

        // Assert
        Assert.Equal(ProjectStatus.Generated, project.Status);
    }

    [Fact]
    public void SetDisplayImage_WithValidData_SetsDisplayImage()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var imageData = new byte[] { 1, 2, 3, 4 };
        var imageProcessor = new FakeImageProcessingService();

        // Act
        project.SetDisplayImage(imageData, imageProcessor);

        // Assert
        Assert.NotNull(project.DisplayImage);
        Assert.NotEmpty(project.DisplayImage);
    }

    [Fact]
    public void SetDisplayImage_WithNullData_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var imageProcessor = new FakeImageProcessingService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => project.SetDisplayImage(null!, imageProcessor));
    }

    [Fact]
    public void SetDisplayImage_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var imageProcessor = new FakeImageProcessingService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => project.SetDisplayImage(Array.Empty<byte>(), imageProcessor));
    }

    [Fact]
    public void SetDisplayImage_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var imageData = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => project.SetDisplayImage(imageData, null!));
    }
}
