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

        public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
        {
            return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>(Array.Empty<TransformationTypeDTO>());
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

    #region Transformation Management Tests

    [Fact]
    public async Task AddTransformationAsync_AddsTransformationAndSaves()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var transformation = new TransformationEntity
        {
            Id = Guid.NewGuid(),
            TransformationType = "TestTransformation",
            DisplayOrder = 0
        };

        // Act
        await project.AddTransformationAsync(transformation);

        // Assert
        Assert.Single(project.Transformations);
        Assert.Contains(transformation, project.Transformations);
        Assert.Equal(0, transformation.DisplayOrder);
    }

    [Fact]
    public async Task AddTransformationAsync_WithNullTransformation_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => project.AddTransformationAsync(null!));
    }

    [Fact]
    public async Task AddTransformationAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var id = Guid.NewGuid();
        var transformation1 = new TransformationEntity { Id = id, TransformationType = "Type1" };
        var transformation2 = new TransformationEntity { Id = id, TransformationType = "Type2" };

        await project.AddTransformationAsync(transformation1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => project.AddTransformationAsync(transformation2));
    }

    [Fact]
    public async Task AddTransformationAsync_SetsDisplayOrderCorrectly()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var t1 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T1" };
        var t2 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T2" };
        var t3 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T3" };

        // Act
        await project.AddTransformationAsync(t1);
        await project.AddTransformationAsync(t2);
        await project.AddTransformationAsync(t3);

        // Assert
        Assert.Equal(0, t1.DisplayOrder);
        Assert.Equal(1, t2.DisplayOrder);
        Assert.Equal(2, t3.DisplayOrder);
    }

    [Fact]
    public async Task RemoveTransformationAsync_RemovesTransformationAndSaves()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var transformation = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "Test" };
        await project.AddTransformationAsync(transformation);

        // Act
        await project.RemoveTransformationAsync(transformation.Id);

        // Assert
        Assert.Empty(project.Transformations);
    }

    [Fact]
    public async Task RemoveTransformationAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => project.RemoveTransformationAsync(nonExistentId));
    }

    [Fact]
    public async Task RemoveTransformationAsync_ReordersRemainingTransformations()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var t1 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T1" };
        var t2 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T2" };
        var t3 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T3" };

        await project.AddTransformationAsync(t1);
        await project.AddTransformationAsync(t2);
        await project.AddTransformationAsync(t3);

        // Act - Remove middle transformation
        await project.RemoveTransformationAsync(t2.Id);

        // Assert
        Assert.Equal(2, project.Transformations.Count);
        Assert.Equal(0, t1.DisplayOrder);
        Assert.Equal(1, t3.DisplayOrder);
    }

    [Fact]
    public async Task ReorderTransformationsAsync_ReordersCorrectlyAndSaves()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var t1 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T1" };
        var t2 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T2" };
        var t3 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T3" };

        await project.AddTransformationAsync(t1);
        await project.AddTransformationAsync(t2);
        await project.AddTransformationAsync(t3);

        // Act - Reverse order
        await project.ReorderTransformationsAsync(new[] { t3.Id, t2.Id, t1.Id });

        // Assert
        Assert.Equal(3, project.Transformations.Count);
        Assert.Equal(t3.Id, project.Transformations[0].Id);
        Assert.Equal(0, project.Transformations[0].DisplayOrder);
        Assert.Equal(t2.Id, project.Transformations[1].Id);
        Assert.Equal(1, project.Transformations[1].DisplayOrder);
        Assert.Equal(t1.Id, project.Transformations[2].Id);
        Assert.Equal(2, project.Transformations[2].DisplayOrder);
    }

    [Fact]
    public async Task ReorderTransformationsAsync_WithNullOrder_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => project.ReorderTransformationsAsync(null!));
    }

    [Fact]
    public async Task ReorderTransformationsAsync_WithWrongCount_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var t1 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T1" };
        var t2 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T2" };

        await project.AddTransformationAsync(t1);
        await project.AddTransformationAsync(t2);

        // Act & Assert - Only one ID in new order
        await Assert.ThrowsAsync<ArgumentException>(() => project.ReorderTransformationsAsync(new[] { t1.Id }));
    }

    [Fact]
    public async Task ReorderTransformationsAsync_WithNonExistentId_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var t1 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T1" };
        var t2 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T2" };

        await project.AddTransformationAsync(t1);
        await project.AddTransformationAsync(t2);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            project.ReorderTransformationsAsync(new[] { t1.Id, nonExistentId }));
    }

    #endregion
}
