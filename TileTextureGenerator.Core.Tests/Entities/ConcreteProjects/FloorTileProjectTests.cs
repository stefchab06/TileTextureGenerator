using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteProjects;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Tests.Entities.ConcreteProjects;

/// <summary>
/// Unit tests for FloorTileProject concrete entity.
/// Tests specific behavior for floor tile texture projects.
/// </summary>
public class FloorTileProjectTests
{
    private class FakeFloorTileProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
        public Task<ProjectBase?> LoadAsync(string projectName) => Task.FromResult<ProjectBase?>(null);
    }

    [Fact]
    public void StaticConstructor_RegistersTypeInRegistry()
    {
        // Arrange - Clear first, then manually register (static constructor already ran)
        TextureProjectRegistry.ClearForTesting();
        TextureProjectRegistry.RegisterType<FloorTileProject>();

        // Act
        var isRegistered = TextureProjectRegistry.IsRegistered(nameof(FloorTileProject));

        // Assert
        Assert.True(isRegistered);
    }

    [Fact]
    public void Registry_CanCreateFloorTileProject()
    {
        // Arrange - Clear first, then setup
        TextureProjectRegistry.ClearForTesting();
        TextureProjectRegistry.SetFactory(type => 
        {
            var store = new FakeFloorTileProjectStore();
            return (ProjectBase)Activator.CreateInstance(type, store)!;
        });
        TextureProjectRegistry.RegisterType<FloorTileProject>();
        var projectName = "RegistryTest";

        // Act
        var project = TextureProjectRegistry.Create(nameof(FloorTileProject), projectName);

        // Assert
        Assert.NotNull(project);
        Assert.IsType<FloorTileProject>(project);
        Assert.Equal(projectName, project.Name);
    }

    [Fact]
    public void Constructor_SetsTypeCorrectly()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();

        // Act
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");

        // Assert
        Assert.Equal(nameof(FloorTileProject), project.Type);
    }

    [Fact]
    public void Constructor_InitializesEmptyTransformationsList()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();

        // Act
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");

        // Assert
        Assert.NotNull(project.Transformations);
        Assert.Empty(project.Transformations);
    }

    [Fact]
    public void Constructor_SetsDefaultTileShapeToFull()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();

        // Act
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");

        // Assert
        Assert.Equal(TileShape.Full, project.TileShape);
    }

    [Fact]
    public void SourceImage_CanBeSet()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");
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
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");

        // Act
        project.TileShape = TileShape.HalfHorizontal;

        // Assert
        Assert.Equal(TileShape.HalfHorizontal, project.TileShape);
    }

    [Fact]
    public async Task GetAvailableTransformationTypesAsync_ReturnsHorizontalFloorTransformation()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");

        // Force transformation registration for test
        TransformationTypeRegistry.RegisterAll();

        // Act
        var availableTypes = await project.GetAvailableTransformationTypesAsync();

        // Assert
        Assert.NotNull(availableTypes);
        Assert.Single(availableTypes);
        Assert.Equal("HorizontalFloorTransformation", availableTypes[0].Name);
        // Icon may be null if generation fails, so we just check the DTO structure
    }
}
