using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;

namespace TileTextureGenerator.Core.Tests.Entities;

/// <summary>
/// Unit tests for FloorTileProject transformation management.
/// Tests AddTransformationAsync, RemoveTransformationAsync, and ReorderTransformationsAsync.
/// </summary>
public class FloorTileProjectTransformationTests
{
    private class FakeFloorTileProjectStore : IProjectStore<FloorTileProject>
    {
        public List<FloorTileProject> SavedProjects { get; } = [];

        public Task SaveAsync(FloorTileProject project)
        {
            SavedProjects.Add(project);
            return Task.CompletedTask;
        }

        public Task<FloorTileProject?> LoadAsync(string projectName) => Task.FromResult<FloorTileProject?>(null);
    }

    [Fact]
    public async Task AddTransformationAsync_AddsTransformationAndSaves()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
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
        Assert.Single(store.SavedProjects); // Verify save was called
    }

    [Fact]
    public async Task AddTransformationAsync_WithNullTransformation_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("TestProject");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => project.AddTransformationAsync(null!));
    }

    [Fact]
    public async Task AddTransformationAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
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
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
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
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("TestProject");
        var transformation = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "Test" };
        await project.AddTransformationAsync(transformation);
        store.SavedProjects.Clear(); // Clear initial save

        // Act
        await project.RemoveTransformationAsync(transformation.Id);

        // Assert
        Assert.Empty(project.Transformations);
        Assert.Single(store.SavedProjects); // Verify save was called
    }

    [Fact]
    public async Task RemoveTransformationAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("TestProject");
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => project.RemoveTransformationAsync(nonExistentId));
    }

    [Fact]
    public async Task RemoveTransformationAsync_ReordersRemainingTransformations()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
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
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("TestProject");
        var t1 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T1" };
        var t2 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T2" };
        var t3 = new TransformationEntity { Id = Guid.NewGuid(), TransformationType = "T3" };
        
        await project.AddTransformationAsync(t1);
        await project.AddTransformationAsync(t2);
        await project.AddTransformationAsync(t3);
        store.SavedProjects.Clear();

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
        Assert.Single(store.SavedProjects);
    }

    [Fact]
    public async Task ReorderTransformationsAsync_WithNullOrder_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("TestProject");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => project.ReorderTransformationsAsync(null!));
    }

    [Fact]
    public async Task ReorderTransformationsAsync_WithWrongCount_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
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
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
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
}
