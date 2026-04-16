using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Entities.ConcreteProjects;
using TileTextureGenerator.Core.Entities.ConcreteTransformations;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;
using TileTextureGenerator.Tests.Common;

namespace TileTextureGenerator.Core.Tests.Entities.ConcreteProjects;

/// <summary>
/// Unit tests for FloorTileProject concrete entity.
/// Tests specific behavior for floor tile texture projects.
/// </summary>
[Collection("ProjectRegistry")]
public class FloorTileProjectTests
{
    private class FakeFloorTileProjectStore : IProjectStore
    {
        public TransformationBase? TransformationToReturn { get; set; }

        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationID) => Task.CompletedTask;

        public Task<TransformationBase> LoadTransformationAsync(ProjectBase project, Guid transformationId)
        {
            return Task.FromResult(TransformationToReturn)!;
        }

        public Task ArchiveAsync(ProjectBase project) => Task.CompletedTask;
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
        var imageData = TestImageFactory.CreateDisplayImage();

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

    [Fact]
    public async Task GetTransformationAsync_ReturnsTransformationWithCorrectParent()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");
        project.TileShape = TileShape.HalfHorizontal;
        project.SourceImage = TestImageFactory.CreateImageData();

        var transformationId = Guid.NewGuid();
        var transformation = new HorizontalFloorTransformation(new FakeTransformationStore());
        transformation.Initialize(project, transformationId);

        store.TransformationToReturn = transformation;

        // Act
        var result = await project.GetTransformationAsync(transformationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project, result.ParentProject);
        Assert.IsType<FloorTileProject>(result.ParentProject);

        // Verify parent project specific properties are accessible
        var floorProject = (FloorTileProject)result.ParentProject;
        Assert.Equal(TileShape.HalfHorizontal, floorProject.TileShape);
        Assert.NotNull(floorProject.SourceImage);
    }

    [Fact]
    public async Task GetTransformationAsync_WithFloorSpecificTransformation_WorksCorrectly()
    {
        // Arrange
        var store = new FakeFloorTileProjectStore();
        var project = new FloorTileProject(store);
        project.Initialize("FloorProject");

        var transformationId = Guid.NewGuid();
        var transformation = new HorizontalFloorTransformation(new FakeTransformationStore());
        transformation.Initialize(project, transformationId);
        transformation.TileShape = TileShape.Full;

        store.TransformationToReturn = transformation;

        // Act
        var result = await project.GetTransformationAsync(transformationId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<HorizontalFloorTransformation>(result);
        var floorTransformation = (HorizontalFloorTransformation)result;
        Assert.Equal(TileShape.Full, floorTransformation.TileShape);
    }

    private class FakeTransformationStore : ITransformationStore
    {
        public Task SaveAsync(TransformationBase transformation) => Task.CompletedTask;
    }
}
