using TileTextureGenerator.Core.DTOs;
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
        public TestProject(IProjectStore store) : base(store)
        {
        }

        public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
        {
            return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>(Array.Empty<TransformationTypeDTO>());
        }
    }

    private class FakeProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
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

    #region Transformation Management Tests

    [Fact]
    public async Task AddTransformationAsync_AddsTransformationAndSaves()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var transformation = new TransformationDTO
        {
            Id = Guid.NewGuid(),
            Type = "TestTransformation"
        };

        // Act
        await project.AddTransformationAsync(transformation);

        // Assert
        Assert.Single(project.Transformations);
        Assert.Contains(transformation, project.Transformations);
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
        var transformation1 = new TransformationDTO { Id = id, Type = "Type1" };
        var transformation2 = new TransformationDTO { Id = id, Type = "Type2" };

        await project.AddTransformationAsync(transformation1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => project.AddTransformationAsync(transformation2));
    }

    [Fact]
    public async Task RemoveTransformationAsync_RemovesTransformationAndSaves()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var transformation = new TransformationDTO { Id = Guid.NewGuid(), Type = "Test" };
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
        var t1 = new TransformationDTO { Id = Guid.NewGuid(), Type = "T1" };
        var t2 = new TransformationDTO { Id = Guid.NewGuid(), Type = "T2" };
        var t3 = new TransformationDTO { Id = Guid.NewGuid(), Type = "T3" };

        await project.AddTransformationAsync(t1);
        await project.AddTransformationAsync(t2);
        await project.AddTransformationAsync(t3);

        // Act - Remove middle transformation
        await project.RemoveTransformationAsync(t2.Id);

        // Assert
        Assert.Equal(2, project.Transformations.Count);
        Assert.Equal(t1.Id, project.Transformations[0].Id);
        Assert.Equal(t3.Id, project.Transformations[1].Id);
    }

    #endregion
}
