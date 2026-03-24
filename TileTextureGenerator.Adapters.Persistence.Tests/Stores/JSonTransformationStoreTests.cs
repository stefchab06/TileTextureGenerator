using System.Text.Json;
using TileTextureGenerator.Adapters.Persistence.Stores;
using TileTextureGenerator.Adapters.Persistence.Tests.Mocks;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteProjects;
using TileTextureGenerator.Core.Entities.ConcreteTransformations;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;
using Xunit;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Stores;

/// <summary>
/// Unit tests for JSonTransformationStore.
/// Tests transformation persistence operations (SaveAsync).
/// </summary>
[Collection("TransformationRegistry")]
public class JSonTransformationStoreTests
{
    private readonly InMemoryFileStorage _fileStorage;
    private readonly JSonTransformationStore _transformationStore;
    private readonly JSonProjectStore _projectStore;

    public JSonTransformationStoreTests()
    {
        _fileStorage = new InMemoryFileStorage();
        _transformationStore = new JSonTransformationStore(_fileStorage);
        _projectStore = new JSonProjectStore(_fileStorage);

        // Setup TransformationTypeRegistry for tests
        TransformationTypeRegistry.Clear();

        TransformationTypeRegistry.SetFactory(type =>
        {
            var transformationStore = new FakeTransformationStore();
            return (TransformationBase)Activator.CreateInstance(type, transformationStore)!;
        });

        TransformationTypeRegistry.Register<HorizontalFloorTransformation>();
        TransformationTypeRegistry.Register<VerticalWallTransformation>();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_SimpleTransformation_SavesCorrectly()
    {
        // Arrange
        var project = new FloorTileProject(_projectStore);
        project.Initialize("TestProject");

        // Create project JSON first
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var initialJson = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, initialJson);

        var transformation = new HorizontalFloorTransformation(_transformationStore);
        var transformationId = Guid.NewGuid();
        transformation.Initialize(project, transformationId);
        transformation.TileShape = TileShape.HalfHorizontal;

        // Act
        await ((ITransformationStore)_transformationStore).SaveAsync(transformation);

        // Assert
        string savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var root = JsonDocument.Parse(savedJson).RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.True(transformationsElement.TryGetProperty(transformationId.ToString(), out var transformationElement));
        Assert.True(transformationElement.TryGetProperty("type", out var typeElement));
        Assert.Equal("HorizontalFloorTransformation", typeElement.GetString());
        Assert.True(transformationElement.TryGetProperty("tileShape", out var tileShapeElement));
        Assert.Equal("HalfHorizontal", tileShapeElement.GetString());
    }

    [Fact]
    public async Task SaveAsync_WithImageData_SavesImageAndPath()
    {
        // Arrange
        var project = new FloorTileProject(_projectStore);
        project.Initialize("TestProject");

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var initialJson = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, initialJson);

        var transformation = new HorizontalFloorTransformation(_transformationStore);
        var transformationId = Guid.NewGuid();
        transformation.Initialize(project, transformationId);

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 }; // PNG header + data
        transformation.BaseTexture = new ImageData(imageData);

        // Act
        await ((ITransformationStore)_transformationStore).SaveAsync(transformation);

        // Assert
        string savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var root = JsonDocument.Parse(savedJson).RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.True(transformationsElement.TryGetProperty(transformationId.ToString(), out var transformationElement));

        // Verify path property exists (fully lowercase + Path)
        Assert.True(transformationElement.TryGetProperty("basetexturePath", out var pathElement));
        string imagePath = pathElement.GetString()!;
        Assert.StartsWith("Workspace/", imagePath);
        Assert.EndsWith(".png", imagePath);

        // Verify image file was created
        var projectDir = _fileStorage.GetProjectPath("TestProject");
        var fullImagePath = Path.Combine(projectDir, imagePath);
        Assert.True(await _fileStorage.FileExistsAsync(fullImagePath));

        // Verify image content
        var savedImageData = await _fileStorage.ReadAllBytesAsync(fullImagePath);
        Assert.Equal(imageData, savedImageData);
    }

    [Fact]
    public async Task SaveAsync_ExistingImageData_ReusesGuid()
    {
        // Arrange
        var project = new FloorTileProject(_projectStore);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var existingGuid = Guid.NewGuid();
        var existingImagePath = $"Workspace/{existingGuid}.png";

        // Create existing JSON with image path
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationId}}": {
              "type": "HorizontalFloorTransformation",
              "basetexturePath": "{{existingImagePath}}"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Create existing image file
        var projectDir = _fileStorage.GetProjectPath("TestProject");
        var oldImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 9, 9, 9 };
        await _fileStorage.WriteAllBytesAsync(Path.Combine(projectDir, existingImagePath), oldImageData);

        var transformation = new HorizontalFloorTransformation(_transformationStore);
        transformation.Initialize(project, transformationId);

        var newImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 }; // Different data
        transformation.BaseTexture = new ImageData(newImageData);

        // Act
        await ((ITransformationStore)_transformationStore).SaveAsync(transformation);

        // Assert
        string savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var root = JsonDocument.Parse(savedJson).RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.True(transformationsElement.TryGetProperty(transformationId.ToString(), out var transformationElement));
        Assert.True(transformationElement.TryGetProperty("basetexturePath", out var pathElement));

        // Verify GUID was reused (same path)
        Assert.Equal(existingImagePath, pathElement.GetString());

        // Verify image was updated with new data
        var savedImageData = await _fileStorage.ReadAllBytesAsync(Path.Combine(projectDir, existingImagePath));
        Assert.Equal(newImageData, savedImageData);
    }

    [Fact]
    public async Task SaveAsync_ExcludesIconAndParentProjectAtRootLevel()
    {
        // Arrange
        var project = new FloorTileProject(_projectStore);
        project.Initialize("TestProject");

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var initialJson = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, initialJson);

        var transformation = new HorizontalFloorTransformation(_transformationStore);
        var transformationId = Guid.NewGuid();
        transformation.Initialize(project, transformationId);

        // Act
        await ((ITransformationStore)_transformationStore).SaveAsync(transformation);

        // Assert
        string savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var root = JsonDocument.Parse(savedJson).RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.True(transformationsElement.TryGetProperty(transformationId.ToString(), out var transformationElement));

        // Verify Icon is NOT saved
        Assert.False(transformationElement.TryGetProperty("icon", out _));
        Assert.False(transformationElement.TryGetProperty("iconPath", out _));

        // Verify ParentProject is NOT saved
        Assert.False(transformationElement.TryGetProperty("parentProject", out _));

        // Verify Id is NOT saved (used as key)
        Assert.False(transformationElement.TryGetProperty("id", out _));

        // Verify Type IS saved
        Assert.True(transformationElement.TryGetProperty("type", out _));
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingTransformation()
    {
        // Arrange
        var project = new FloorTileProject(_projectStore);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");

        // Create existing transformation
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationId}}": {
              "type": "HorizontalFloorTransformation",
              "tileShape": "Full"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        var transformation = new HorizontalFloorTransformation(_transformationStore);
        transformation.Initialize(project, transformationId);
        transformation.TileShape = TileShape.HalfVertical; // Different value

        // Act
        await ((ITransformationStore)_transformationStore).SaveAsync(transformation);

        // Assert
        string savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var root = JsonDocument.Parse(savedJson).RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.True(transformationsElement.TryGetProperty(transformationId.ToString(), out var transformationElement));
        Assert.True(transformationElement.TryGetProperty("tileShape", out var tileShapeElement));

        // Verify value was updated
        Assert.Equal("HalfVertical", tileShapeElement.GetString());
    }

    [Fact]
    public async Task SaveAsync_PreservesOtherTransformations()
    {
        // Arrange
        var project = new FloorTileProject(_projectStore);
        project.Initialize("TestProject");

        var transformation1Id = Guid.NewGuid();
        var transformation2Id = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");

        // Create existing transformations
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformation1Id}}": {
              "type": "HorizontalFloorTransformation",
              "tileShape": "Full"
            },
            "{{transformation2Id}}": {
              "type": "VerticalFloorTransformation",
              "tileShape": "HalfHorizontal"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Update only transformation1
        var transformation = new HorizontalFloorTransformation(_transformationStore);
        transformation.Initialize(project, transformation1Id);
        transformation.TileShape = TileShape.HalfVertical;

        // Act
        await ((ITransformationStore)_transformationStore).SaveAsync(transformation);

        // Assert
        string savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var root = JsonDocument.Parse(savedJson).RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));

        // Verify transformation1 was updated
        Assert.True(transformationsElement.TryGetProperty(transformation1Id.ToString(), out var transformation1Element));
        Assert.True(transformation1Element.TryGetProperty("tileShape", out var tileShape1Element));
        Assert.Equal("HalfVertical", tileShape1Element.GetString());

        // Verify transformation2 was preserved
        Assert.True(transformationsElement.TryGetProperty(transformation2Id.ToString(), out var transformation2Element));
        Assert.True(transformation2Element.TryGetProperty("type", out var type2Element));
        Assert.Equal("VerticalFloorTransformation", type2Element.GetString());
        Assert.True(transformation2Element.TryGetProperty("tileShape", out var tileShape2Element));
        Assert.Equal("HalfHorizontal", tileShape2Element.GetString());
    }

    #endregion

    private class FakeTransformationStore : ITransformationStore
    {
        public Task SaveAsync(TransformationBase transformation) => Task.CompletedTask;
    }
}
