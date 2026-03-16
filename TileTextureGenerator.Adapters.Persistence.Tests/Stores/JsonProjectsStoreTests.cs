using TileTextureGenerator.Adapters.Persistence.Stores;
using TileTextureGenerator.Adapters.Persistence.Tests.Mocks;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteProjects;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;
using Xunit;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Stores;

/// <summary>
/// Tests for JSON-based projects store implementation.
/// </summary>
public class JsonProjectsStoreTests : IDisposable
{
    private readonly InMemoryFileStorage _fileStorage;
    private readonly JsonProjectsStore _store;
    private readonly MockProjectStore _mockProjectStore;

    public JsonProjectsStoreTests()
    {
        _fileStorage = new InMemoryFileStorage();
        _store = new JsonProjectsStore(_fileStorage);
        _mockProjectStore = new MockProjectStore();
        
        // Setup registry factory for tests
        TextureProjectRegistry.SetFactory(type =>
        {
            if (type == typeof(FloorTileProject))
                return new FloorTileProject(_mockProjectStore);
            
            throw new InvalidOperationException($"Unsupported type: {type.Name}");
        });
    }

    public void Dispose()
    {
        _fileStorage.Clear();
    }

    [Fact]
    public async Task SaveAsync_WithValidProject_CreatesDirectoryStructure()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        project.Status = ProjectStatus.New;

        // Act
        await _store.SaveAsync(project);

        // Assert
        string expectedDir = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject");
        Assert.True(await _fileStorage.DirectoryExistsAsync(expectedDir));
        Assert.True(await _fileStorage.DirectoryExistsAsync(Path.Combine(expectedDir, "Sources")));
        Assert.True(await _fileStorage.DirectoryExistsAsync(Path.Combine(expectedDir, "Workspace")));
        Assert.True(await _fileStorage.DirectoryExistsAsync(Path.Combine(expectedDir, "Outputs")));
    }

    [Fact]
    public async Task SaveAsync_WithValidProject_CreatesJsonFile()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        project.Status = ProjectStatus.New;
        project.TileShape = TileShape.Full;

        // Act
        await _store.SaveAsync(project);

        // Assert
        string expectedJsonPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "TestProject.json");
        Assert.True(await _fileStorage.FileExistsAsync(expectedJsonPath));
        
        string json = await _fileStorage.ReadAllTextAsync(expectedJsonPath);
        Assert.Contains("\"name\": \"TestProject\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"type\": \"FloorTileProject\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status\": \"New\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"tileShape\": \"Full\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithDisplayImage_SavesImageFile()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47]; // PNG header
        project.DisplayImage = imageData;

        // Act
        await _store.SaveAsync(project);

        // Assert
        string expectedImagePath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "Sources", "DisplayImage.png");
        Assert.True(await _fileStorage.FileExistsAsync(expectedImagePath));
        
        byte[] savedImage = await _fileStorage.ReadAllBytesAsync(expectedImagePath);
        Assert.Equal(imageData, savedImage);
    }

    [Fact]
    public async Task SaveAsync_WithSourceImage_SavesImageFile()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        byte[] sourceImageData = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A];
        project.SourceImage = sourceImageData;

        // Act
        await _store.SaveAsync(project);

        // Assert
        string expectedImagePath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "Sources", "SourceImage.png");
        Assert.True(await _fileStorage.FileExistsAsync(expectedImagePath));
        
        byte[] savedImage = await _fileStorage.ReadAllBytesAsync(expectedImagePath);
        Assert.Equal(sourceImageData, savedImage);
    }

    [Fact]
    public async Task SaveAsync_WithInvalidCharactersInName_CleansFileName()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("Test<Project>:Name*");

        // Act
        await _store.SaveAsync(project);

        // Assert
        string expectedDir = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "Test_Project__Name_");
        Assert.True(await _fileStorage.DirectoryExistsAsync(expectedDir));
    }

    [Fact]
    public async Task SaveAsync_WithConflictingCleanedName_ThrowsException()
    {
        // Arrange
        var project1 = new FloorTileProject(_mockProjectStore);
        project1.Initialize("Project<1>");
        await _store.SaveAsync(project1);

        var project2 = new FloorTileProject(_mockProjectStore);
        project2.Initialize("Project:1:");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveAsync(project2));
        Assert.Contains("similar name already exists", exception.Message);
        Assert.Contains("Project<1>", exception.Message);
    }

    [Fact]
    public async Task LoadAsync_WithNonExistentProject_ReturnsNull()
    {
        // Act
        var result = await _store.LoadAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadAsync_WithExistingProject_ReturnsProject()
    {
        // Arrange
        var originalProject = new FloorTileProject(_mockProjectStore);
        originalProject.Initialize("TestProject");
        originalProject.Status = ProjectStatus.Pending;
        originalProject.TileShape = TileShape.HalfHorizontal;
        await _store.SaveAsync(originalProject);

        // Act
        var loadedProject = await _store.LoadAsync("TestProject");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal("TestProject", loadedProject.Name);
        Assert.Equal("FloorTileProject", loadedProject.Type);
        Assert.Equal(ProjectStatus.Pending, loadedProject.Status);
        
        var floorProject = Assert.IsType<FloorTileProject>(loadedProject);
        Assert.Equal(TileShape.HalfHorizontal, floorProject.TileShape);
    }

    [Fact]
    public async Task LoadAsync_WithDisplayImage_LoadsImageData()
    {
        // Arrange
        var originalProject = new FloorTileProject(_mockProjectStore);
        originalProject.Initialize("TestProject");
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];
        originalProject.DisplayImage = imageData;
        await _store.SaveAsync(originalProject);

        // Act
        var loadedProject = await _store.LoadAsync("TestProject");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.NotNull(loadedProject.DisplayImage);
        Assert.Equal(imageData, loadedProject.DisplayImage);
    }

    [Fact]
    public async Task LoadAsync_WithSourceImage_LoadsImageData()
    {
        // Arrange
        var originalProject = new FloorTileProject(_mockProjectStore);
        originalProject.Initialize("TestProject");
        byte[] sourceImageData = [0x89, 0x50, 0x4E, 0x47, 0x0D];
        originalProject.SourceImage = sourceImageData;
        await _store.SaveAsync(originalProject);

        // Act
        var loadedProject = await _store.LoadAsync("TestProject");

        // Assert
        Assert.NotNull(loadedProject);
        var floorProject = Assert.IsType<FloorTileProject>(loadedProject);
        Assert.NotNull(floorProject.SourceImage);
        Assert.Equal(sourceImageData, floorProject.SourceImage);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingProject_RemovesDirectoryAndContents()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        await _store.SaveAsync(project);
        
        string projectDir = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject");
        Assert.True(await _fileStorage.DirectoryExistsAsync(projectDir));

        // Act
        await _store.DeleteAsync("TestProject");

        // Assert
        Assert.False(await _fileStorage.DirectoryExistsAsync(projectDir));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentProject_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        await _store.DeleteAsync("NonExistent");
    }

    [Fact]
    public async Task ExistsAsync_WithExistingProject_ReturnsTrue()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        await _store.SaveAsync(project);

        // Act
        bool exists = await _store.ExistsAsync("TestProject");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentProject_ReturnsFalse()
    {
        // Act
        bool exists = await _store.ExistsAsync("NonExistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithDirectoryButNoJson_ReturnsFalse()
    {
        // Arrange
        string projectDir = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject");
        await _fileStorage.EnsureDirectoryExistsAsync(projectDir);

        // Act
        bool exists = await _store.ExistsAsync("TestProject");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ListProjectsAsync_WithNoProjects_ReturnsEmptyList()
    {
        // Act
        var projects = await _store.ListProjectsAsync();

        // Assert
        Assert.Empty(projects);
    }

    [Fact]
    public async Task ListProjectsAsync_WithMultipleProjects_ReturnsAllValidProjects()
    {
        // Arrange
        var project1 = new FloorTileProject(_mockProjectStore);
        project1.Initialize("Project1");
        project1.Status = ProjectStatus.Generated;
        await _store.SaveAsync(project1);

        var project2 = new FloorTileProject(_mockProjectStore);
        project2.Initialize("Project2");
        project2.Status = ProjectStatus.Pending;
        await _store.SaveAsync(project2);

        // Act
        var projects = await _store.ListProjectsAsync();

        // Assert
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.Name == "Project1" && p.Status == ProjectStatus.Generated);
        Assert.Contains(projects, p => p.Name == "Project2" && p.Status == ProjectStatus.Pending);
    }

    [Fact]
    public async Task ListProjectsAsync_WithInvalidDirectories_SkipsThem()
    {
        // Arrange
        var validProject = new FloorTileProject(_mockProjectStore);
        validProject.Initialize("ValidProject");
        await _store.SaveAsync(validProject);

        // Create invalid directory (no JSON file)
        string invalidDir = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "InvalidDir");
        await _fileStorage.EnsureDirectoryExistsAsync(invalidDir);

        // Act
        var projects = await _store.ListProjectsAsync();

        // Assert
        Assert.Single(projects);
        Assert.Equal("ValidProject", projects[0].Name);
    }

    [Fact]
    public async Task SaveAsync_WithNullProject_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.SaveAsync(null!));
    }

    [Fact]
    public async Task LoadAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.LoadAsync(null!));
    }

    [Fact]
    public async Task LoadAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _store.LoadAsync(string.Empty));
    }

    [Fact]
    public async Task SaveAsync_WithTransformations_PreservesThemInJson()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");

        // Add transformations to the project
        var transformation1 = new TransformationDTO 
        { 
            Id = Guid.NewGuid(), 
            Type = "HorizontalFloorTransformation",
            Icon = [0x01, 0x02]
        };
        var transformation2 = new TransformationDTO 
        { 
            Id = Guid.NewGuid(), 
            Type = "VerticalWallTransformation",
            Icon = [0x03, 0x04]
        };
        project.Transformations.Add(transformation1);
        project.Transformations.Add(transformation2);

        // Act - Save the project
        await _store.SaveAsync(project);

        // Assert - Transformations must be preserved in the JSON file
        string jsonPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "TestProject.json");
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);

        // Verify transformations array is in JSON
        Assert.Contains("\"transformations\"", jsonContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(transformation1.Id.ToString(), jsonContent);
        Assert.Contains(transformation2.Id.ToString(), jsonContent);
        Assert.Contains("HorizontalFloorTransformation", jsonContent);
        Assert.Contains("VerticalWallTransformation", jsonContent);
    }

    [Fact]
    public async Task SaveAsync_WithExistingTransformationsInJson_DoesNotDeleteThem()
    {
        // Arrange - Create a project with transformations
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        var originalTransformation = new TransformationDTO 
        { 
            Id = Guid.NewGuid(), 
            Type = "HorizontalFloorTransformation",
            Icon = [0x01, 0x02]
        };
        project.Transformations.Add(originalTransformation);
        await _store.SaveAsync(project);

        // Get the JSON and verify transformation is there
        string jsonPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "TestProject.json");
        string originalJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        Assert.Contains(originalTransformation.Id.ToString(), originalJson);

        // Act - Save the same project again with updated property (simulating an update)
        project.Status = ProjectStatus.Generated; // Change something else
        await _store.SaveAsync(project);

        // Assert - Transformations should STILL be in the JSON (not deleted)
        string updatedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        Assert.Contains("\"transformations\"", updatedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(originalTransformation.Id.ToString(), updatedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HorizontalFloorTransformation", updatedJson);
    }

    // TODO: Reactivate this test when TransformationsStore is implemented
    [Fact(Skip = "Transformations loading not yet implemented - will be handled by TransformationsStore")]
    public async Task LoadAsync_WithTransformations_LoadsTransformationsFromJson()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("TestProject");
        var transformation1 = new TransformationDTO 
        { 
            Id = Guid.NewGuid(), 
            Type = "HorizontalFloorTransformation",
            Icon = [0x01, 0x02]
        };
        var transformation2 = new TransformationDTO 
        { 
            Id = Guid.NewGuid(), 
            Type = "VerticalWallTransformation",
            Icon = [0x03, 0x04]
        };
        project.Transformations.Add(transformation1);
        project.Transformations.Add(transformation2);
        await _store.SaveAsync(project);

        // Act
        var loadedProject = await _store.LoadAsync("TestProject");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal(2, loadedProject.Transformations.Count);
        Assert.Contains(loadedProject.Transformations, t => t.Id == transformation1.Id);
        Assert.Contains(loadedProject.Transformations, t => t.Id == transformation2.Id);
    }

    // Mock store for testing
    private class MockProjectStore : IProjectStore<FloorTileProject>
    {
        public Task SaveAsync(FloorTileProject entity) => Task.CompletedTask;
        public Task<FloorTileProject?> LoadAsync(string name) => Task.FromResult<FloorTileProject?>(null);
        public Task DeleteAsync(string name) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string name) => Task.FromResult(false);
    }
}
