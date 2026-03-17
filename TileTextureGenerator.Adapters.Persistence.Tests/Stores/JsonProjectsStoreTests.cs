using System.Text.Json;
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
/// Tests focus on simple DTO-based persistence (CreateProjectAsync).
/// Complex polymorphic persistence will be handled by IProjectStore.
/// </summary>
[Collection("Sequential")]
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

        // Force auto-registration of project types (triggers static constructors)
        TextureProjectRegistry.ForceAutoRegistration(typeof(FloorTileProject).Assembly);
    }

    public void Dispose()
    {
        _fileStorage.Clear();
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidDto_CreatesDirectoryStructure()
    {
        // Arrange
        var dto = new ProjectDto(
            name: "TestProject",
            type: "FloorTileProject",
            status: ProjectStatus.New,
            lastModifiedDate: DateTime.UtcNow
        );

        // Act
        await _store.CreateProjectAsync(dto);

        // Assert
        string expectedDir = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject");
        Assert.True(await _fileStorage.DirectoryExistsAsync(expectedDir));
        Assert.True(await _fileStorage.DirectoryExistsAsync(Path.Combine(expectedDir, "Sources")));
        Assert.True(await _fileStorage.DirectoryExistsAsync(Path.Combine(expectedDir, "Workspace")));
        Assert.True(await _fileStorage.DirectoryExistsAsync(Path.Combine(expectedDir, "Outputs")));
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidDto_CreatesJsonFile()
    {
        // Arrange
        var dto = new ProjectDto(
            name: "TestProject",
            type: "FloorTileProject",
            status: ProjectStatus.New,
            lastModifiedDate: DateTime.UtcNow
        );

        // Act
        await _store.CreateProjectAsync(dto);

        // Assert
        string expectedJsonPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "TestProject.json");
        Assert.True(await _fileStorage.FileExistsAsync(expectedJsonPath));
        
        string json = await _fileStorage.ReadAllTextAsync(expectedJsonPath);
        Assert.Contains("\"name\": \"TestProject\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"type\": \"FloorTileProject\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status\": \"New\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateProjectAsync_WithDisplayImage_SavesImageFile()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47]; // PNG header
        var dto = new ProjectDto(
            name: "TestProject",
            type: "FloorTileProject",
            status: ProjectStatus.New,
            lastModifiedDate: DateTime.UtcNow,
            displayImage: imageData
        );

        // Act
        await _store.CreateProjectAsync(dto);

        // Assert
        string expectedImagePath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "Sources", "DisplayImage.png");
        Assert.True(await _fileStorage.FileExistsAsync(expectedImagePath));
        
        byte[] savedImage = await _fileStorage.ReadAllBytesAsync(expectedImagePath);
        Assert.Equal(imageData, savedImage);
    }

    [Fact]
    public async Task CreateProjectAsync_WithInvalidCharactersInName_CleansFileName()
    {
        // Arrange
        var dto = new ProjectDto(
            name: "Test<Project>:Name*",
            type: "FloorTileProject",
            status: ProjectStatus.New,
            lastModifiedDate: DateTime.UtcNow
        );

        // Act
        await _store.CreateProjectAsync(dto);

        // Assert
        string expectedDir = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "Test_Project__Name_");
        Assert.True(await _fileStorage.DirectoryExistsAsync(expectedDir));
    }

    [Fact]
    public async Task CreateProjectAsync_WithConflictingCleanedName_ThrowsException()
    {
        // Arrange
        var dto1 = new ProjectDto("Project<1>", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow);
        await _store.CreateProjectAsync(dto1);

        var dto2 = new ProjectDto("Project:1:", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _store.CreateProjectAsync(dto2));
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
    public async Task LoadAsync_WithExistingProject_ReturnsBasicProject()
    {
        // Arrange
        var dto = new ProjectDto(
            name: "TestProject",
            type: "FloorTileProject",
            status: ProjectStatus.Pending,
            lastModifiedDate: DateTime.UtcNow
        );
        await _store.CreateProjectAsync(dto);

        // Manually add a polymorphic property to JSON (simulating IProjectStore behavior)
        string jsonPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "TestProject.json");
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
        jsonDoc!["tileShape"] = "HalfHorizontal"; // FloorTileProject specific property
        string updatedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);

        // Act
        var loadedProject = await _store.LoadAsync("TestProject");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal("TestProject", loadedProject.Name);
        Assert.Equal("FloorTileProject", loadedProject.Type);
        Assert.Equal(ProjectStatus.Pending, loadedProject.Status);

        var floorProject = Assert.IsType<FloorTileProject>(loadedProject);
        Assert.Equal(TileShape.HalfHorizontal, floorProject.TileShape); // Polymorphic property loaded
    }

    [Fact]
    public async Task LoadAsync_WithDisplayImage_LoadsImageData()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];
        var dto = new ProjectDto(
            name: "TestProject",
            type: "FloorTileProject",
            status: ProjectStatus.New,
            lastModifiedDate: DateTime.UtcNow,
            displayImage: imageData
        );
        await _store.CreateProjectAsync(dto);

        // Act
        var loadedProject = await _store.LoadAsync("TestProject");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.NotNull(loadedProject.DisplayImage);
        Assert.Equal(imageData, loadedProject.DisplayImage);
    }

    [Fact]
    public async Task SaveAsync_WithProjectBase_SavesPolymorphically()
    {
        // Arrange
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("PolymorphicTest");
        project.Status = ProjectStatus.Pending;
        project.TileShape = TileShape.HalfVertical;

        // Act
        await ((IProjectStore)_store).SaveAsync(project);

        // Assert - Verify JSON contains polymorphic properties
        string jsonPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "PolymorphicTest", "PolymorphicTest.json");
        Assert.True(await _fileStorage.FileExistsAsync(jsonPath));

        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        Assert.Contains("\"name\": \"PolymorphicTest\"", jsonContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"type\": \"FloorTileProject\"", jsonContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status\": \"Pending\"", jsonContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"tileShape\": \"HalfVertical\"", jsonContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithSourceImage_SavesImageFile()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("ImageTest");
        project.SourceImage = imageData;

        // Act
        await ((IProjectStore)_store).SaveAsync(project);

        // Assert
        string imagePath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "ImageTest", "Sources", "SourceImage.png");
        Assert.True(await _fileStorage.FileExistsAsync(imagePath));

        byte[] savedImage = await _fileStorage.ReadAllBytesAsync(imagePath);
        Assert.Equal(imageData, savedImage);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingProject_RemovesDirectoryAndContents()
    {
        // Arrange
        var dto = new ProjectDto("TestProject", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow);
        await _store.CreateProjectAsync(dto);
        
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
        var dto = new ProjectDto("TestProject", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow);
        await _store.CreateProjectAsync(dto);

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
        var dto1 = new ProjectDto("Project1", "FloorTileProject", ProjectStatus.Generated, DateTime.UtcNow);
        var dto2 = new ProjectDto("Project2", "FloorTileProject", ProjectStatus.Pending, DateTime.UtcNow);
        await _store.CreateProjectAsync(dto1);
        await _store.CreateProjectAsync(dto2);

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
        var validDto = new ProjectDto("ValidProject", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow);
        await _store.CreateProjectAsync(validDto);

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
    public async Task CreateProjectAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.CreateProjectAsync(null!));
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
    public async Task LoadAsync_WithTransformations_LoadsTransformationsList()
    {
        // Arrange
        var dto = new ProjectDto("TestProject", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow);
        await _store.CreateProjectAsync(dto);

        // Manually add transformations to JSON (simulating what IProjectStore will do)
        string jsonPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "TileTextureGenerator", "Projects", "TestProject", "TestProject.json");
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

        var transformation1Id = Guid.NewGuid();
        var transformation2Id = Guid.NewGuid();

        jsonDoc!["transformations"] = new[]
        {
            new { id = transformation1Id, type = "HorizontalFloorTransformation", icon = (byte[]?)null },
            new { id = transformation2Id, type = "VerticalWallTransformation", icon = (byte[]?)null }
        };

        string updatedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);

        // Act
        var loadedProject = await _store.LoadAsync("TestProject");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal(2, loadedProject.Transformations.Count);
        Assert.Contains(loadedProject.Transformations, t => t.Id == transformation1Id);
        Assert.Contains(loadedProject.Transformations, t => t.Id == transformation2Id);
        Assert.Contains(loadedProject.Transformations, t => t.Type == "HorizontalFloorTransformation");
        Assert.Contains(loadedProject.Transformations, t => t.Type == "VerticalWallTransformation");
    }

    // Mock store for testing
    private class MockProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase entity) => Task.CompletedTask;
    }
}
