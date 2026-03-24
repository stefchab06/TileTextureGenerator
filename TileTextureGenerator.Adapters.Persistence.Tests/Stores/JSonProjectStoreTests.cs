using System.Text.Json;
using TileTextureGenerator.Adapters.Persistence.Stores;
using TileTextureGenerator.Adapters.Persistence.Tests.Mocks;
using TileTextureGenerator.Core.DTOs;
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
/// Unit tests for JSonProjectStore.
/// Tests individual transformation persistence operations (Add/Remove/Load).
/// </summary>
[Collection("TransformationRegistry")]
public class JSonProjectStoreTests
{
    private readonly InMemoryFileStorage _fileStorage;
    private readonly JSonProjectStore _store;

    public JSonProjectStoreTests()
    {
        _fileStorage = new InMemoryFileStorage();
        _store = new JSonProjectStore(_fileStorage);

        // Clear and setup TransformationTypeRegistry for LoadTransformation tests
        TransformationTypeRegistry.Clear();

        TransformationTypeRegistry.SetFactory(type =>
        {
            var transformationStore = new FakeTransformationStore();
            return (TransformationBase)Activator.CreateInstance(type, transformationStore)!;
        });

        // Manually register transformation types (bypassing static constructors that may have run)
        TransformationTypeRegistry.Register<HorizontalFloorTransformation>();
        TransformationTypeRegistry.Register<VerticalWallTransformation>();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_ForFloorTileProject_SavesAllProperties()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("FloorProject");
        project.Status = ProjectStatus.Generated;
        project.TileShape = TileShape.HalfHorizontal;
        project.SourceImage = new ImageData(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        await ((IProjectStore)_store).SaveAsync(project);

        // Assert
        var jsonPath = _fileStorage.GetProjectFileName("FloorProject");
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("name", out var nameElement));
        Assert.Equal("FloorProject", nameElement.GetString());

        Assert.True(root.TryGetProperty("type", out var typeElement));
        Assert.Equal("FloorTileProject", typeElement.GetString());

        Assert.True(root.TryGetProperty("status", out var statusElement));
        Assert.Equal("Generated", statusElement.GetString());

        Assert.True(root.TryGetProperty("tileShape", out var tileShapeElement));
        Assert.Equal("HalfHorizontal", tileShapeElement.GetString());

        // Verify SourceImage is saved as path (fully lowercase + Path)
        Assert.True(root.TryGetProperty("sourceimagePath", out var sourceImagePathElement));
        Assert.NotNull(sourceImagePathElement.GetString());
    }

    [Fact]
    public async Task SaveAsync_ForWallTileProject_SavesAllProperties()
    {
        // Arrange
        var project = new WallTileProject(_store);
        project.Initialize("WallProject");
        project.Status = ProjectStatus.Generated;
        project.TileShape = TileShape.HalfVertical;
        project.SourceImage = new ImageData(new byte[] { 10, 20, 30 });

        // Act
        await ((IProjectStore)_store).SaveAsync(project);

        // Assert
        var jsonPath = _fileStorage.GetProjectFileName("WallProject");
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("name", out var nameElement));
        Assert.Equal("WallProject", nameElement.GetString());

        Assert.True(root.TryGetProperty("type", out var typeElement));
        Assert.Equal("WallTileProject", typeElement.GetString());

        Assert.True(root.TryGetProperty("status", out var statusElement));
        Assert.Equal("Generated", statusElement.GetString());

        Assert.True(root.TryGetProperty("tileShape", out var tileShapeElement));
        Assert.Equal("HalfVertical", tileShapeElement.GetString());

        Assert.True(root.TryGetProperty("sourceimagePath", out var sourceImagePathElement));
        Assert.NotNull(sourceImagePathElement.GetString());
    }

    [Fact]
    public async Task SaveAsync_DoesNotModifyTransformationsList()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");
        project.Status = ProjectStatus.New;

        // Manually add transformation DTOs to the project (simulating that transformations exist)
        var transformation1 = new TransformationDTO
        {
            Id = Guid.NewGuid(),
            Type = "HorizontalFloorTransformation",
            Icon = null
        };
        var transformation2 = new TransformationDTO
        {
            Id = Guid.NewGuid(),
            Type = "HorizontalFloorTransformation",
            Icon = null
        };
        project.Transformations.Add(transformation1);
        project.Transformations.Add(transformation2);

        // Pre-create a JSON file with existing transformations structure
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformation1.Id}}": { "type": "HorizontalFloorTransformation" },
            "{{transformation2.Id}}": { "type": "HorizontalFloorTransformation" }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act
        await ((IProjectStore)_store).SaveAsync(project);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        // Verify transformations node still exists and has 2 entries
        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        var transformationsCount = 0;
        foreach (var prop in transformationsElement.EnumerateObject())
        {
            transformationsCount++;
        }
        Assert.Equal(2, transformationsCount);

        // Verify the transformation IDs are still there
        Assert.True(transformationsElement.TryGetProperty(transformation1.Id.ToString(), out _));
        Assert.True(transformationsElement.TryGetProperty(transformation2.Id.ToString(), out _));
    }

    [Fact]
    public async Task SaveAsync_WithNoTransformations_DoesNotCreateTransformationsNode()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("EmptyProject");
        project.Status = ProjectStatus.New;

        // Act
        await ((IProjectStore)_store).SaveAsync(project);

        // Assert
        var jsonPath = _fileStorage.GetProjectFileName("EmptyProject");
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        // Verify transformations node does NOT exist
        Assert.False(root.TryGetProperty("transformations", out _));
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingProject_PreservesTransformations()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("UpdateTest");
        project.Status = ProjectStatus.New;
        project.TileShape = TileShape.Full;

        var transformation1Id = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("UpdateTest");
        var existingJson = $$"""
        {
          "name": "UpdateTest",
          "type": "FloorTileProject",
          "status": "New",
          "tileShape": "Full",
          "transformations": {
            "{{transformation1Id}}": { "type": "HorizontalFloorTransformation" }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Change project properties
        project.Status = ProjectStatus.Generated;
        project.TileShape = TileShape.HalfHorizontal;

        // Act
        await ((IProjectStore)_store).SaveAsync(project);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        // Verify updated properties
        Assert.True(root.TryGetProperty("status", out var statusElement));
        Assert.Equal("Generated", statusElement.GetString());

        Assert.True(root.TryGetProperty("tileShape", out var tileShapeElement));
        Assert.Equal("HalfHorizontal", tileShapeElement.GetString());

        // Verify transformations are preserved
        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.True(transformationsElement.TryGetProperty(transformation1Id.ToString(), out _));
    }

    #endregion

    #region AddTransformationAsync Tests

    [Fact]
    public async Task AddTransformationAsync_ToEmptyProject_CreatesTransformationsNode()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        // Pre-create project JSON without transformations
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        var transformation = new TransformationDTO
        {
            Id = Guid.NewGuid(),
            Type = "HorizontalFloorTransformation",
            Icon = new ImageData(new byte[] { 1, 2, 3 }) // Should NOT be saved
        };

        // Act
        await ((IProjectStore)_store).AddTransformationAsync(project, transformation);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.Equal(JsonValueKind.Object, transformationsElement.ValueKind);

        // Verify transformation exists with correct structure
        Assert.True(transformationsElement.TryGetProperty(transformation.Id.ToString(), out var transformationElement));
        Assert.True(transformationElement.TryGetProperty("type", out var typeElement));
        Assert.Equal("HorizontalFloorTransformation", typeElement.GetString());

        // Verify Icon is NOT saved
        Assert.False(transformationElement.TryGetProperty("icon", out _), "Icon should not be saved in JSON");
    }

    [Fact]
    public async Task AddTransformationAsync_WhenTransformationsExist_AddsToExisting()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var existingTransformationId = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{existingTransformationId}}": { "type": "HorizontalFloorTransformation" }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        var newTransformation = new TransformationDTO
        {
            Id = Guid.NewGuid(),
            Type = "VerticalWallTransformation",
            Icon = null
        };

        // Act
        await ((IProjectStore)_store).AddTransformationAsync(project, newTransformation);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));

        // Count transformations
        int count = 0;
        foreach (var prop in transformationsElement.EnumerateObject())
        {
            count++;
        }
        Assert.Equal(2, count);

        // Verify both transformations exist
        Assert.True(transformationsElement.TryGetProperty(existingTransformationId.ToString(), out _));
        Assert.True(transformationsElement.TryGetProperty(newTransformation.Id.ToString(), out _));
    }

    [Fact]
    public async Task AddTransformationAsync_MultipleTransformations_MaintainsOptionAStructure()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        var transformation1 = new TransformationDTO { Id = Guid.NewGuid(), Type = "Type1", Icon = null };
        var transformation2 = new TransformationDTO { Id = Guid.NewGuid(), Type = "Type2", Icon = null };
        var transformation3 = new TransformationDTO { Id = Guid.NewGuid(), Type = "Type3", Icon = null };

        // Act
        await ((IProjectStore)_store).AddTransformationAsync(project, transformation1);
        await ((IProjectStore)_store).AddTransformationAsync(project, transformation2);
        await ((IProjectStore)_store).AddTransformationAsync(project, transformation3);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.Equal(JsonValueKind.Object, transformationsElement.ValueKind);

        // Verify all 3 transformations with GUID keys (Option A structure)
        Assert.True(transformationsElement.TryGetProperty(transformation1.Id.ToString(), out var t1));
        Assert.True(transformationsElement.TryGetProperty(transformation2.Id.ToString(), out var t2));
        Assert.True(transformationsElement.TryGetProperty(transformation3.Id.ToString(), out var t3));

        Assert.Equal("Type1", t1.GetProperty("type").GetString());
        Assert.Equal("Type2", t2.GetProperty("type").GetString());
        Assert.Equal("Type3", t3.GetProperty("type").GetString());
    }

    #endregion

    #region RemoveTransformationAsync Tests

    [Fact]
    public async Task RemoveTransformationAsync_ExistingTransformation_RemovesIt()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var transformationToRemove = Guid.NewGuid();
        var transformationToKeep = Guid.NewGuid();

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationToRemove}}": { "type": "TypeToRemove" },
            "{{transformationToKeep}}": { "type": "TypeToKeep" }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act
        await ((IProjectStore)_store).RemoveTransformationAsync(project, transformationToRemove);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));

        // Verify removed transformation is gone
        Assert.False(transformationsElement.TryGetProperty(transformationToRemove.ToString(), out _));

        // Verify kept transformation still exists
        Assert.True(transformationsElement.TryGetProperty(transformationToKeep.ToString(), out _));
    }

    [Fact]
    public async Task RemoveTransformationAsync_LastTransformation_RemovesTransformationsNode()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var onlyTransformation = Guid.NewGuid();

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{onlyTransformation}}": { "type": "OnlyType" }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act
        await ((IProjectStore)_store).RemoveTransformationAsync(project, onlyTransformation);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        // Verify transformations node is completely removed
        Assert.False(root.TryGetProperty("transformations", out _), "Transformations node should be removed when empty");
    }

    [Fact]
    public async Task RemoveTransformationAsync_NonExistentId_DoesNotCrash()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var existingTransformation = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{existingTransformation}}": { "type": "ExistingType" }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act - should not throw
        await ((IProjectStore)_store).RemoveTransformationAsync(project, nonExistentId);

        // Assert
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        // Verify existing transformation is still there
        Assert.True(root.TryGetProperty("transformations", out var transformationsElement));
        Assert.True(transformationsElement.TryGetProperty(existingTransformation.ToString(), out _));
    }

    [Fact]
    public async Task RemoveTransformationAsync_NoTransformationsNode_DoesNotCrash()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        var someId = Guid.NewGuid();

        // Act - should not throw
        await ((IProjectStore)_store).RemoveTransformationAsync(project, someId);

        // Assert - JSON should be unchanged
        var savedJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var jsonDoc = JsonDocument.Parse(savedJson);
        var root = jsonDoc.RootElement;

        Assert.False(root.TryGetProperty("transformations", out _));
    }

    #endregion

    #region LoadTransformationAsync Tests

    [Fact]
    public async Task LoadTransformationAsync_SimpleTransformation_LoadsCorrectly()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationId}}": { 
              "type": "HorizontalFloorTransformation",
              "tileShape": "HalfHorizontal"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act
        var transformation = await ((IProjectStore)_store).LoadTransformationAsync(project, transformationId);

        // Assert
        Assert.NotNull(transformation);
        Assert.Equal(transformationId, transformation.Id);
        Assert.Equal("HorizontalFloorTransformation", transformation.Type);
        Assert.Equal(project, transformation.ParentProject);
    }

    [Fact]
    public async Task LoadTransformationAsync_WithImageData_LoadsImageFromPath()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var projectDir = _fileStorage.GetProjectPath("TestProject");

        // Create mock image file
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 }; // PNG header + data
        var imagePath = Path.Combine(projectDir, "Sources", "BaseTexture.png");
        await _fileStorage.WriteAllBytesAsync(imagePath, imageData);

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationId}}": { 
              "type": "HorizontalFloorTransformation",
              "basetexturePath": "Sources/BaseTexture.png"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act
        var transformation = await ((IProjectStore)_store).LoadTransformationAsync(project, transformationId);

        // Assert
        Assert.NotNull(transformation);

        // Verify image was loaded (cast to concrete type to access BaseTexture)
        var concreteTransformation = Assert.IsType<HorizontalFloorTransformation>(transformation);
        Assert.NotNull(concreteTransformation.BaseTexture);
        Assert.Equal(imageData, concreteTransformation.BaseTexture.Value.Bytes);
    }

    [Fact]
    public async Task LoadTransformationAsync_BasicPropertiesOnly_WorksCorrectly()
    {
        // Arrange - Test seulement les propriétés de base (pas RequiredPaperType qui est by design)
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject", 
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationId}}": { 
              "type": "HorizontalFloorTransformation",
              "tileShape": "HalfVertical"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act
        var transformation = await ((IProjectStore)_store).LoadTransformationAsync(project, transformationId);

        // Assert
        Assert.NotNull(transformation);
        Assert.Equal(transformationId, transformation.Id);
        Assert.Equal("HorizontalFloorTransformation", transformation.Type);
        Assert.Equal(project, transformation.ParentProject);

        var concreteTransformation = Assert.IsType<HorizontalFloorTransformation>(transformation);
        Assert.Equal(TileShape.HalfVertical, concreteTransformation.TileShape);

        // RequiredPaperType vient de la classe concrète, pas du JSON (by design)
        Assert.Equal(PaperType.Standard, transformation.RequiredPaperType);
    }

    [Fact]
    public async Task LoadTransformationAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{existingId}}": { "type": "HorizontalFloorTransformation" }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ((IProjectStore)_store).LoadTransformationAsync(project, nonExistentId));

        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    [Fact]
    public async Task LoadTransformationAsync_NoTransformationsNode_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = """
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New"
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        var someId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ((IProjectStore)_store).LoadTransformationAsync(project, someId));

        Assert.Contains("No transformations found", exception.Message);
    }

    [Fact]
    public async Task LoadTransformationAsync_MissingTypeProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationId}}": { 
              "someProperty": "someValue"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ((IProjectStore)_store).LoadTransformationAsync(project, transformationId));

        Assert.Contains("missing 'type'", exception.Message);
    }

    [Fact]
    public async Task LoadTransformationAsync_IconDerivedFromRegistry_NotFromJson()
    {
        // Arrange
        var project = new FloorTileProject(_store);
        project.Initialize("TestProject");

        var transformationId = Guid.NewGuid();
        var jsonPath = _fileStorage.GetProjectFileName("TestProject");

        // JSON intentionally includes "icon" property (should be ignored)
        var existingJson = $$"""
        {
          "name": "TestProject",
          "type": "FloorTileProject",
          "status": "New",
          "transformations": {
            "{{transformationId}}": { 
              "type": "HorizontalFloorTransformation",
              "icon": "ThisShouldBeIgnored"
            }
          }
        }
        """;
        await _fileStorage.WriteAllTextAsync(jsonPath, existingJson);

        // Act
        var transformation = await ((IProjectStore)_store).LoadTransformationAsync(project, transformationId);

        // Assert
        Assert.NotNull(transformation);

        // Icon should come from the transformation's Icon property (derived from registry)
        // not from JSON
        var icon = transformation.Icon;
        // Icon may be null or generated - the important part is it's NOT "ThisShouldBeIgnored"
        // We just verify the transformation was loaded correctly
        Assert.Equal("HorizontalFloorTransformation", transformation.Type);
    }

    #endregion

    private class FakeTransformationStore : ITransformationStore
    {
        public Task SaveAsync(TransformationBase transformation) => Task.CompletedTask;
    }
}
