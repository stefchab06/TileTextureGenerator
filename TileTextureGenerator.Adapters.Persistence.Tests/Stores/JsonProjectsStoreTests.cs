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
        string expectedDir = _fileStorage.GetProjectPath("TestProject");
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
        string expectedJsonPath = _fileStorage.GetProjectFileName("TestProject");
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
        string expectedImagePath = Path.Combine(_fileStorage.GetProjectPath("TestProject"), "Sources", "DisplayImage.png");
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
        string expectedDir = _fileStorage.GetProjectPath("Test_Project__Name_");
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
        string jsonPath = _fileStorage.GetProjectFileName("TestProject");
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
    public async Task DeleteAsync_WithExistingProject_RemovesDirectoryAndContents()
    {
        // Arrange
        var dto = new ProjectDto("TestProject", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow);
        await _store.CreateProjectAsync(dto);

        string projectDir = _fileStorage.GetProjectPath("TestProject");
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
        string projectDir = _fileStorage.GetProjectPath("TestProject");
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
        string jsonPath = _fileStorage.GetProjectFileName("TestProject");
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

    [Fact]
    public async Task SaveAndLoad_WithPolymorphicProperties_PersistsFloorTileProjectSpecificData()
    {
        // Arrange - Create a FloorTileProject with specific properties
        var project = new FloorTileProject(_mockProjectStore);
        project.Initialize("PolymorphicTest");
        project.TileShape = TileShape.HalfHorizontal;
        project.Status = ProjectStatus.Pending;

        // Convert to DTO and save
        var dto = new ProjectDto(
            name: project.Name,
            type: project.Type,
            status: project.Status,
            lastModifiedDate: DateTime.UtcNow
        );
        await _store.CreateProjectAsync(dto);

        // Manually add polymorphic property to JSON (simulating IProjectStore behavior)
        string jsonPath = _fileStorage.GetProjectFileName("PolymorphicTest");
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Add FloorTileProject-specific property
        jsonDoc!["tileShape"] = JsonSerializer.SerializeToElement(TileShape.HalfHorizontal, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        string updatedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);

        // Act - Load the project back
        var loadedProject = await _store.LoadAsync("PolymorphicTest");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.IsType<FloorTileProject>(loadedProject);

        var floorProject = (FloorTileProject)loadedProject;
        Assert.Equal("PolymorphicTest", floorProject.Name);
        Assert.Equal("FloorTileProject", floorProject.Type);
        Assert.Equal(ProjectStatus.Pending, floorProject.Status);
        Assert.Equal(TileShape.HalfHorizontal, floorProject.TileShape);
    }

    [Fact]
    public async Task SaveAndLoad_WithMultipleTransformations_PreservesOrderAndAllProperties()
    {
        // Arrange - Create project with multiple transformations
        var transformation1Id = Guid.NewGuid();
        var transformation2Id = Guid.NewGuid();
        var transformation3Id = Guid.NewGuid();

        var dto = new ProjectDto("TransformOrderTest", "FloorTileProject", ProjectStatus.Pending, DateTime.UtcNow);
        await _store.CreateProjectAsync(dto);

        // Add transformations with specific order to JSON
        string jsonPath = _fileStorage.GetProjectFileName("TransformOrderTest");
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

        // Create transformations array with specific order (order = index in array)
        jsonDoc!["transformations"] = new[]
        {
            new { id = transformation1Id, type = "HorizontalFloorTransformation", icon = (byte[]?)null },
            new { id = transformation2Id, type = "VerticalWallTransformation", icon = (byte[]?)null },
            new { id = transformation3Id, type = "HorizontalFloorTransformation", icon = (byte[]?)null }
        };

        string updatedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);

        // Act
        var loadedProject = await _store.LoadAsync("TransformOrderTest");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal(3, loadedProject.Transformations.Count);

        // Verify order is preserved (index 0, 1, 2)
        Assert.Equal(transformation1Id, loadedProject.Transformations[0].Id);
        Assert.Equal("HorizontalFloorTransformation", loadedProject.Transformations[0].Type);

        Assert.Equal(transformation2Id, loadedProject.Transformations[1].Id);
        Assert.Equal("VerticalWallTransformation", loadedProject.Transformations[1].Type);

        Assert.Equal(transformation3Id, loadedProject.Transformations[2].Id);
        Assert.Equal("HorizontalFloorTransformation", loadedProject.Transformations[2].Type);
    }

    [Fact]
    public async Task SaveAndLoad_WithMultipleImages_LoadsAllImageProperties()
    {
        // Arrange - Create different image data for DisplayImage and SourceImage
        byte[] displayImageData = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]; // PNG header + extra
        byte[] sourceImageData = [0x89, 0x50, 0x4E, 0x47, 0xFF, 0xAA, 0xBB, 0xCC]; // Different PNG data

        var dto = new ProjectDto(
            name: "MultiImageTest",
            type: "FloorTileProject",
            status: ProjectStatus.Pending,
            lastModifiedDate: DateTime.UtcNow,
            displayImage: displayImageData
        );

        await _store.CreateProjectAsync(dto);

        // Manually add SourceImage to file system and JSON path
        string projectDir = _fileStorage.GetProjectPath("MultiImageTest");
        string sourceImagePath = Path.Combine(projectDir, "Sources", "SourceImage.png");
        await _fileStorage.WriteAllBytesAsync(sourceImagePath, sourceImageData);

        // Update JSON to include sourceImagePath
        string jsonPath = _fileStorage.GetProjectFileName("MultiImageTest");
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        jsonDoc!["sourceImagePath"] = JsonSerializer.SerializeToElement("Sources/SourceImage.png", new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        string updatedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);

        // Act - Load the project
        var loadedProject = await _store.LoadAsync("MultiImageTest");

        // Assert
        Assert.NotNull(loadedProject);
        Assert.IsType<FloorTileProject>(loadedProject);

        var floorProject = (FloorTileProject)loadedProject;

        // Verify DisplayImage is loaded correctly
        Assert.NotNull(floorProject.DisplayImage);
        Assert.Equal(displayImageData, floorProject.DisplayImage.Value.Bytes);

        // Verify SourceImage is loaded correctly
        Assert.NotNull(floorProject.SourceImage);
        Assert.Equal(sourceImageData, floorProject.SourceImage.Value.Bytes);
    }

    [Fact]
    public async Task SaveAndLoad_CompleteProject_AllPropertiesPreserved()
    {
        // Arrange - Create a complete project with all properties set
        byte[] displayImageData = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        byte[] sourceImageData = [0x89, 0x50, 0x4E, 0x47, 0xFF, 0xAA, 0xBB, 0xCC];

        var transformation1Id = Guid.NewGuid();
        var transformation2Id = Guid.NewGuid();

        var originalLastModified = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);

        // Create DTO with all basic properties
        var dto = new ProjectDto(
            name: "CompleteProject",
            type: "FloorTileProject",
            status: ProjectStatus.Generated,
            lastModifiedDate: originalLastModified,
            displayImage: displayImageData
        );

        await _store.CreateProjectAsync(dto);

        // Add all polymorphic properties and images to JSON
        string jsonPath = _fileStorage.GetProjectFileName("CompleteProject");
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Add FloorTileProject-specific property
        jsonDoc!["tileShape"] = JsonSerializer.SerializeToElement(TileShape.HalfVertical, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Add transformations
        jsonDoc["transformations"] = JsonSerializer.SerializeToElement(new[]
        {
            new { id = transformation1Id, type = "HorizontalFloorTransformation", icon = (byte[]?)null },
            new { id = transformation2Id, type = "HorizontalFloorTransformation", icon = (byte[]?)null }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Add SourceImage path
        string projectDir = _fileStorage.GetProjectPath("CompleteProject");
        string sourceImagePath = Path.Combine(projectDir, "Sources", "SourceImage.png");
        await _fileStorage.WriteAllBytesAsync(sourceImagePath, sourceImageData);
        jsonDoc["sourceImagePath"] = JsonSerializer.SerializeToElement("Sources/SourceImage.png", new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Save updated JSON
        string updatedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);

        // Act - Load the project
        var loadedProject = await _store.LoadAsync("CompleteProject");

        // Assert - Verify ALL properties are preserved
        Assert.NotNull(loadedProject);
        Assert.IsType<FloorTileProject>(loadedProject);

        var floorProject = (FloorTileProject)loadedProject;

        // Basic properties
        Assert.Equal("CompleteProject", floorProject.Name);
        Assert.Equal("FloorTileProject", floorProject.Type);
        Assert.Equal(ProjectStatus.Generated, floorProject.Status);
        Assert.Equal(originalLastModified, floorProject.LastModifiedDate);

        // Polymorphic property
        Assert.Equal(TileShape.HalfVertical, floorProject.TileShape);

        // Images
        Assert.NotNull(floorProject.DisplayImage);
        Assert.Equal(displayImageData, floorProject.DisplayImage.Value.Bytes);
        Assert.NotNull(floorProject.SourceImage);
        Assert.Equal(sourceImageData, floorProject.SourceImage.Value.Bytes);

        // Transformations
        Assert.Equal(2, floorProject.Transformations.Count);
        Assert.Equal(transformation1Id, floorProject.Transformations[0].Id);
        Assert.Equal("HorizontalFloorTransformation", floorProject.Transformations[0].Type);
        Assert.Equal(transformation2Id, floorProject.Transformations[1].Id);
        Assert.Equal("HorizontalFloorTransformation", floorProject.Transformations[1].Type);
    }

    // Mock store for testing
    private class MockProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase entity) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationID) => Task.CompletedTask;
    }
}
