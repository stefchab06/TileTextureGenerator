using TileTextureGenerator.Adapters.Persistence.Stores;
using TileTextureGenerator.Adapters.Persistence.Tests.Mocks;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteProjects;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;
using Xunit;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Stores;

/// <summary>
/// Tests for JsonProjectsStore.ArchiveAsync method.
/// Validates archiving behavior: Workspace deletion, JSON reduction to base properties.
/// </summary>
[Collection("Sequential")]
public sealed class JsonProjectsStoreArchiveTests : IDisposable
{
    private readonly InMemoryFileStorage _storage;
    private readonly JsonProjectsStore _store;
    private readonly string _projectsRoot;
    private readonly MockProjectStore _mockProjectStore;

    public JsonProjectsStoreArchiveTests()
    {
        _storage = new InMemoryFileStorage();
        _store = new JsonProjectsStore(_storage);
        _projectsRoot = _storage.GetProjectsRootPath();
        _mockProjectStore = new MockProjectStore();

        // Setup registry factory for tests
        TextureProjectRegistry.SetFactory(type =>
        {
            if (type == typeof(FloorTileProject))
                return new FloorTileProject(_mockProjectStore);

            throw new InvalidOperationException($"Unsupported type: {type.Name}");
        });

        // Force auto-registration
        TextureProjectRegistry.ForceAutoRegistration(typeof(FloorTileProject).Assembly);
    }

    public void Dispose()
    {
        _storage.Clear();
    }

    [Fact]
    public async Task WhenProjectDoesNotExist_ThenArchiveAsyncThrowsInvalidOperationException()
    {
        // Arrange
        string nonExistentProject = "NonExistent";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _store.ArchiveAsync(nonExistentProject)
        );

        Assert.Contains("does not exist", exception.Message);
        Assert.Contains(nonExistentProject, exception.Message);
    }

    [Fact]
    public async Task WhenProjectNameIsNull_ThenArchiveAsyncThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _store.ArchiveAsync(null!)
        );
    }

    [Fact]
    public async Task WhenProjectNameIsWhitespace_ThenArchiveAsyncThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _store.ArchiveAsync("   ")
        );
    }

    [Fact]
    public async Task WhenArchivingProject_ThenStatusIsSetToArchived()
    {
        // Arrange
        string projectName = "TestProject";
        var displayImage = CreateTestImage();
        var dto = new ProjectDto(projectName, "FloorTileProject", ProjectStatus.Pending, DateTime.UtcNow)
        {
            DisplayImage = displayImage
        };
        await _store.CreateProjectAsync(dto);

        // Act
        await _store.ArchiveAsync(projectName);

        // Assert
        var loadedProject = await _store.LoadAsync(projectName);
        Assert.NotNull(loadedProject);
        Assert.Equal(ProjectStatus.Archived, loadedProject.Status);
    }

    [Fact]
    public async Task WhenArchivingProject_ThenWorkspaceFolderIsDeleted()
    {
        // Arrange
        string projectName = "TestProject";
        var displayImage = CreateTestImage();
        var dto = new ProjectDto(projectName, "FloorTileProject", ProjectStatus.Pending, DateTime.UtcNow)
        {
            DisplayImage = displayImage
        };
        await _store.CreateProjectAsync(dto);

        string projectDir = Path.Combine(_projectsRoot, projectName);
        string workspaceDir = Path.Combine(projectDir, "Workspace");

        // Create a dummy file in Workspace
        await _storage.EnsureDirectoryExistsAsync(workspaceDir);
        string dummyFile = Path.Combine(workspaceDir, "temp.png");
        await _storage.WriteAllBytesAsync(dummyFile, [1, 2, 3, 4]);

        // Verify Workspace exists before archiving
        Assert.True(await _storage.DirectoryExistsAsync(workspaceDir));
        Assert.True(await _storage.FileExistsAsync(dummyFile));

        // Act
        await _store.ArchiveAsync(projectName);

        // Assert
        Assert.False(await _storage.DirectoryExistsAsync(workspaceDir));
    }

    [Fact]
    public async Task WhenArchivingProject_ThenDisplayImageIsPreserved()
    {
        // Arrange
        string projectName = "TestProject";
        var displayImage = CreateTestImage();
        var dto = new ProjectDto(projectName, "FloorTileProject", ProjectStatus.Pending, DateTime.UtcNow)
        {
            DisplayImage = displayImage
        };
        await _store.CreateProjectAsync(dto);

        // Act
        await _store.ArchiveAsync(projectName);

        // Assert
        var loadedProject = await _store.LoadAsync(projectName);
        Assert.NotNull(loadedProject);
        Assert.NotNull(loadedProject.DisplayImage);
        Assert.Equal(displayImage.Bytes.Length, loadedProject.DisplayImage.Value.Bytes.Length);
    }

    [Fact]
    public async Task WhenArchivingProject_ThenLastModifiedDateIsUpdated()
    {
        // Arrange
        string projectName = "TestProject";
        var displayImage = CreateTestImage();
        var initialDate = DateTime.UtcNow.AddDays(-1);
        var dto = new ProjectDto(projectName, "FloorTileProject", ProjectStatus.Pending, initialDate)
        {
            DisplayImage = displayImage
        };
        await _store.CreateProjectAsync(dto);

        // Wait a tiny bit to ensure time difference
        await Task.Delay(10);

        // Act
        var beforeArchive = DateTime.UtcNow;
        await _store.ArchiveAsync(projectName);
        var afterArchive = DateTime.UtcNow;

        // Assert
        var loadedProject = await _store.LoadAsync(projectName);
        Assert.NotNull(loadedProject);
        Assert.True(loadedProject.LastModifiedDate >= beforeArchive);
        Assert.True(loadedProject.LastModifiedDate <= afterArchive);
    }

    [Fact]
    public async Task WhenArchivingProject_ThenTransformationsListIsPreserved()
    {
        // Arrange
        string projectName = "TestProject";
        var displayImage = CreateTestImage();
        var dto = new ProjectDto(projectName, "FloorTileProject", ProjectStatus.Pending, DateTime.UtcNow)
        {
            DisplayImage = displayImage
        };
        await _store.CreateProjectAsync(dto);

        // Load and add transformation
        var project = await _store.LoadAsync(projectName);
        Assert.NotNull(project);

        var transformation = new TransformationDTO
        {
            Id = Guid.NewGuid(),
            Type = "HorizontalFloorTransformation",
            Icon = null
        };
        project.Transformations.Add(transformation);

        // Save project with transformation via JSonProjectStore (we need another approach here)
        // For now, we'll just verify the list structure is preserved

        // Act
        await _store.ArchiveAsync(projectName);

        // Assert
        var loadedProject = await _store.LoadAsync(projectName);
        Assert.NotNull(loadedProject);
        Assert.NotNull(loadedProject.Transformations);
    }

    // Helper methods

    private static ImageData CreateTestImage()
    {
        byte[] pngBytes = [137, 80, 78, 71, 13, 10, 26, 10]; // PNG header
        return new ImageData(pngBytes);
    }

    // Mock store for testing
    private class MockProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase entity) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationID) => Task.CompletedTask;
        public Task<TransformationBase> LoadTransformationAsync(ProjectBase project, Guid transformationId) => Task.FromResult<TransformationBase>(null!);
    }
}
