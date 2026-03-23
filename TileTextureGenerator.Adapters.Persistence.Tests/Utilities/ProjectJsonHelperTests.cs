using System.Text.Json;
using System.Text.Json.Nodes;
using TileTextureGenerator.Adapters.Persistence.Tests.Mocks;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using Xunit;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Utilities;

public class ProjectJsonHelperTests
{
    private readonly InMemoryFileStorage _fileStorage;
    private readonly ProjectJsonHelper _helper;

    private static readonly JsonSerializerOptions TestJsonOptions = new()
    {
        WriteIndented = true
    };

    public ProjectJsonHelperTests()
    {
        _fileStorage = new InMemoryFileStorage();
        _helper = new ProjectJsonHelper(_fileStorage);
    }

    [Fact]
    public async Task LoadProjectJsonAsync_FileExists_ReturnsJsonObject()
    {
        // Arrange
        string projectName = "TestProject";
        var jsonContent = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        
        var jsonPath = _fileStorage.GetProjectFileName(projectName);
        await _fileStorage.WriteAllTextAsync(jsonPath, jsonContent);

        // Act
        var result = await _helper.LoadProjectJsonAsync(projectName);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TryGetPropertyValue("name", out var nameNode));
        Assert.Equal("TestProject", nameNode?.GetValue<string>());
        Assert.True(result.TryGetPropertyValue("type", out var typeNode));
        Assert.Equal("FloorTileProject", typeNode?.GetValue<string>());
    }

    [Fact]
    public async Task LoadProjectJsonAsync_FileDoesNotExist_ReturnsEmptyJsonObject()
    {
        // Arrange
        string projectName = "NonExistentProject";

        // Act
        var result = await _helper.LoadProjectJsonAsync(projectName);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // Empty JsonObject has no properties
    }

    [Fact]
    public async Task LoadProjectJsonAsync_EmptyFile_ReturnsEmptyJsonObject()
    {
        // Arrange
        string projectName = "EmptyProject";
        var jsonPath = _fileStorage.GetProjectFileName(projectName);
        await _fileStorage.WriteAllTextAsync(jsonPath, "");

        // Act
        var result = await _helper.LoadProjectJsonAsync(projectName);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadProjectJsonAsync_WhitespaceFile_ReturnsEmptyJsonObject()
    {
        // Arrange
        string projectName = "WhitespaceProject";
        var jsonPath = _fileStorage.GetProjectFileName(projectName);
        await _fileStorage.WriteAllTextAsync(jsonPath, "   \n\t  ");

        // Act
        var result = await _helper.LoadProjectJsonAsync(projectName);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadProjectJsonAsync_NullProjectName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _helper.LoadProjectJsonAsync(null!));
    }

    [Fact]
    public async Task LoadProjectJsonAsync_EmptyProjectName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _helper.LoadProjectJsonAsync(""));
        
        Assert.Contains("Project name cannot be empty or whitespace", exception.Message);
    }

    [Fact]
    public async Task LoadProjectJsonAsync_WhitespaceProjectName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _helper.LoadProjectJsonAsync("   "));
        
        Assert.Contains("Project name cannot be empty or whitespace", exception.Message);
    }

    [Fact]
    public async Task SaveProjectJsonAsync_ValidJsonObject_SavesCorrectly()
    {
        // Arrange
        string projectName = "SaveTest";
        var jsonDoc = new JsonObject
        {
            ["name"] = "SaveTest",
            ["type"] = "FloorTileProject",
            ["status"] = "Generated"
        };

        // Act
        await _helper.SaveProjectJsonAsync(projectName, jsonDoc, TestJsonOptions);

        // Assert
        var jsonPath = _fileStorage.GetProjectFileName(projectName);
        Assert.True(await _fileStorage.FileExistsAsync(jsonPath));
        
        string savedContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var savedJson = JsonNode.Parse(savedContent)?.AsObject();
        
        Assert.NotNull(savedJson);
        Assert.True(savedJson.TryGetPropertyValue("name", out var nameNode));
        Assert.Equal("SaveTest", nameNode?.GetValue<string>());
        Assert.True(savedJson.TryGetPropertyValue("status", out var statusNode));
        Assert.Equal("Generated", statusNode?.GetValue<string>());
    }

    [Fact]
    public async Task SaveProjectJsonAsync_OverwritesExistingFile()
    {
        // Arrange
        string projectName = "OverwriteTest";
        var jsonPath = _fileStorage.GetProjectFileName(projectName);
        
        // Write initial content
        var initialJson = new JsonObject { ["version"] = 1 };
        await _helper.SaveProjectJsonAsync(projectName, initialJson, TestJsonOptions);

        // Act - Overwrite with new content
        var updatedJson = new JsonObject { ["version"] = 2, ["status"] = "Updated" };
        await _helper.SaveProjectJsonAsync(projectName, updatedJson, TestJsonOptions);

        // Assert
        string savedContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var savedJson = JsonNode.Parse(savedContent)?.AsObject();
        
        Assert.NotNull(savedJson);
        Assert.True(savedJson.TryGetPropertyValue("version", out var versionNode));
        Assert.Equal(2, versionNode?.GetValue<int>());
        Assert.True(savedJson.TryGetPropertyValue("status", out var statusNode));
        Assert.Equal("Updated", statusNode?.GetValue<string>());
    }

    [Fact]
    public async Task SaveProjectJsonAsync_NullProjectName_ThrowsArgumentNullException()
    {
        // Arrange
        var jsonDoc = new JsonObject();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _helper.SaveProjectJsonAsync(null!, jsonDoc, TestJsonOptions));
    }

    [Fact]
    public async Task SaveProjectJsonAsync_NullJsonDoc_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _helper.SaveProjectJsonAsync("Test", null!, TestJsonOptions));
    }

    [Fact]
    public async Task SaveProjectJsonAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var jsonDoc = new JsonObject();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _helper.SaveProjectJsonAsync("Test", jsonDoc, null!));
    }

    [Fact]
    public async Task SaveProjectJsonAsync_EmptyProjectName_ThrowsArgumentException()
    {
        // Arrange
        var jsonDoc = new JsonObject();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _helper.SaveProjectJsonAsync("", jsonDoc, TestJsonOptions));
        
        Assert.Contains("Project name cannot be empty or whitespace", exception.Message);
    }

    [Fact]
    public async Task LoadAndSave_RoundTrip_PreservesData()
    {
        // Arrange
        string projectName = "RoundTripTest";
        var originalJson = new JsonObject
        {
            ["name"] = "RoundTripTest",
            ["type"] = "WallTileProject",
            ["status"] = "Pending",
            ["data"] = new JsonObject
            {
                ["nested"] = true,
                ["value"] = 42
            }
        };

        // Act - Save then Load
        await _helper.SaveProjectJsonAsync(projectName, originalJson, TestJsonOptions);
        var loadedJson = await _helper.LoadProjectJsonAsync(projectName);

        // Assert - Data is preserved
        Assert.True(loadedJson.TryGetPropertyValue("name", out var nameNode));
        Assert.Equal("RoundTripTest", nameNode?.GetValue<string>());
        
        Assert.True(loadedJson.TryGetPropertyValue("data", out var dataNode));
        var dataObj = dataNode?.AsObject();
        Assert.NotNull(dataObj);
        Assert.True(dataObj.TryGetPropertyValue("value", out var valueNode));
        Assert.Equal(42, valueNode?.GetValue<int>());
    }
}
