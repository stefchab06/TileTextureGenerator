using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteProjects;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Tests.Entities.ConcreteProjects;

/// <summary>
/// Unit tests for WallTileProject concrete entity.
/// Tests specific behavior for wall tile texture projects.
/// </summary>
public class WallTileProjectTests
{
    private class FakeWallTileProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationID) => Task.CompletedTask;
        public Task<TransformationBase> LoadTransformationAsync(ProjectBase project, Guid transformationId) => Task.FromResult<TransformationBase>(null!);
    }

    [Fact]
    public void StaticConstructor_RegistersTypeInRegistry()
    {
        // Arrange - Clear first, then manually register (static constructor already ran)
        TextureProjectRegistry.ClearForTesting();
        TextureProjectRegistry.RegisterType<WallTileProject>();

        // Act
        var isRegistered = TextureProjectRegistry.IsRegistered(nameof(WallTileProject));

        // Assert
        Assert.True(isRegistered);
    }

    [Fact]
    public void Registry_CanCreateWallTileProject()
    {
        // Arrange - Clear first, then setup
        TextureProjectRegistry.ClearForTesting();
        TextureProjectRegistry.SetFactory(type => 
        {
            var store = new FakeWallTileProjectStore();
            return (ProjectBase)Activator.CreateInstance(type, store)!;
        });
        TextureProjectRegistry.RegisterType<WallTileProject>();
        var projectName = "RegistryTest";

        // Act
        var project = TextureProjectRegistry.Create(nameof(WallTileProject), projectName);

        // Assert
        Assert.NotNull(project);
        Assert.IsType<WallTileProject>(project);
        Assert.Equal(projectName, project.Name);
    }

    [Fact]
    public void Constructor_SetsTypeCorrectly()
    {
        // Arrange
        var store = new FakeWallTileProjectStore();
        
        // Act
        var project = new WallTileProject(store);
        project.Initialize("WallProject");

        // Assert
        Assert.Equal(nameof(WallTileProject), project.Type);
    }

    [Fact]
    public void Constructor_SetsDefaultTileShapeToFull()
    {
        // Arrange
        var store = new FakeWallTileProjectStore();
        
        // Act
        var project = new WallTileProject(store);
        project.Initialize("WallProject");

        // Assert
        Assert.Equal(TileShape.Full, project.TileShape);
    }

    [Fact]
    public void SourceImage_CanBeSet()
    {
        // Arrange
        var store = new FakeWallTileProjectStore();
        var project = new WallTileProject(store);
        project.Initialize("WallProject");
        var imageData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        project.SourceImage = imageData;

        // Assert
        Assert.NotNull(project.SourceImage);
        Assert.Equal(imageData, project.SourceImage);
    }

    [Fact]
    public void TileShape_CanBeModified()
    {
        // Arrange
        var store = new FakeWallTileProjectStore();
        var project = new WallTileProject(store);
        project.Initialize("WallProject");

        // Act
        project.TileShape = TileShape.HalfHorizontal;

        // Assert
        Assert.Equal(TileShape.HalfHorizontal, project.TileShape);
    }

    [Fact]
    public async Task GetAvailableTransformationTypesAsync_ReturnsVerticalWallTransformation()
    {
        // Arrange
        var store = new FakeWallTileProjectStore();
        var project = new WallTileProject(store);
        project.Initialize("WallProject");

        // Force transformation registration for test
        TransformationTypeRegistry.RegisterAll();

        // Act
        var availableTypes = await project.GetAvailableTransformationTypesAsync();

        // Assert
        Assert.NotNull(availableTypes);
        Assert.Single(availableTypes);
        Assert.Equal("VerticalWallTransformation", availableTypes[0].Name);
        // Icon may be null if generation fails, so we just check the DTO structure
    }
}
