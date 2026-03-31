using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
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
        public List<Guid> LoadedTransformationIds { get; } = [];
        public TransformationBase? TransformationToReturn { get; set; }

        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationID) => Task.CompletedTask;

        public Task<TransformationBase> LoadTransformationAsync(ProjectBase project, Guid transformationId)
        {
            LoadedTransformationIds.Add(transformationId);
            return Task.FromResult(TransformationToReturn)!;
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

    #region Transformation Management Tests

    [Fact]
    public async Task AddTransformationAsync_AddsTransformationAndSaves()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        var transformationType = "TestTransformation";

        // Act
        await project.AddTransformationAsync(transformationType);

        // Assert
        Assert.Single(project.Transformations);
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
    public async Task RemoveTransformationAsync_RemovesTransformationAndSaves()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");
        await project.AddTransformationAsync("Test");
        var transformation = project.Transformations.First();

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
    public async Task GetTransformationAsync_WithExistingId_ReturnsTransformation()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var expectedTransformation = new TestTransformation(new FakeTransformationStore());
        expectedTransformation.Initialize(project, transformationId);

        store.TransformationToReturn = expectedTransformation;

        // Act
        var result = await project.GetTransformationAsync(transformationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTransformation, result);
        Assert.Single(store.LoadedTransformationIds);
        Assert.Equal(transformationId, store.LoadedTransformationIds[0]);
    }

    [Fact]
    public async Task GetTransformationAsync_WhenStoreReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        store.TransformationToReturn = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => project.GetTransformationAsync(transformationId));
        Assert.Contains(transformationId.ToString(), exception.Message);
    }

    [Fact]
    public async Task GetTransformationAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeProjectStore();
        var project = new TestProject(store);
        project.Initialize("TestProject");

        var nonExistentId = Guid.NewGuid();
        store.TransformationToReturn = null;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => project.GetTransformationAsync(nonExistentId));
    }

    #endregion

    private sealed class TestTransformation : TransformationBase, ITransformationMetadata
    {
        public TestTransformation(ITransformationStore store) : base(store) { }
        public static string IconResourceName => "Test.png";
        protected override Task<ImageData> ExecuteAsync() => Task.FromResult(new ImageData(Array.Empty<byte>()));
    }

    private class FakeTransformationStore : ITransformationStore
    {
        public Task SaveAsync(TransformationBase transformation) => Task.CompletedTask;
    }
}
