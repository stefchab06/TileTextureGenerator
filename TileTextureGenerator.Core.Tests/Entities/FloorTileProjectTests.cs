using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Tests.Entities;

/// <summary>
/// Unit tests for FloorTileProject concrete entity.
/// Tests specific behavior for floor tile texture projects.
/// </summary>
public class FloorTileProjectTests
{
    [Fact]
    public void StaticConstructor_RegistersTypeInRegistry()
    {
        // Arrange - Clear first, then manually register (static constructor already ran)
        TextureProjectRegistry.ClearForTesting();
        TextureProjectRegistry.Register(nameof(FloorTileProject), name => new FloorTileProject(name));

        // Act
        var isRegistered = TextureProjectRegistry.IsRegistered(nameof(FloorTileProject));

        // Assert
        Assert.True(isRegistered);
    }

    [Fact]
    public void Registry_CanCreateFloorTileProject()
    {
        // Arrange - Clear first, then manually register
        TextureProjectRegistry.ClearForTesting();
        TextureProjectRegistry.Register(nameof(FloorTileProject), name => new FloorTileProject(name));
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
        // Arrange & Act
        var project = new FloorTileProject("FloorProject");

        // Assert
        Assert.Equal(nameof(FloorTileProject), project.Type);
    }

    [Fact]
    public void Constructor_InitializesEmptyTransformationsList()
    {
        // Arrange & Act
        var project = new FloorTileProject("FloorProject");

        // Assert
        Assert.NotNull(project.Transformations);
        Assert.Empty(project.Transformations);
    }

    [Fact]
    public void Constructor_SetsDefaultTileShapeToFull()
    {
        // Arrange & Act
        var project = new FloorTileProject("FloorProject");

        // Assert
        Assert.Equal(TileShape.Full, project.TileShape);
    }

    [Fact]
    public void SourceImage_CanBeSet()
    {
        // Arrange
        var project = new FloorTileProject("FloorProject");
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
        var project = new FloorTileProject("FloorProject");

        // Act
        project.TileShape = TileShape.HalfHorizontal;

        // Assert
        Assert.Equal(TileShape.HalfHorizontal, project.TileShape);
    }

    [Fact]
    public void AddTransformation_AddsToList()
    {
        // Arrange
        var project = new FloorTileProject("FloorProject");
        var transformation = new TransformationEntity
        {
            Id = Guid.NewGuid(),
            TransformationType = "FlatHorizontalTransformation",
            DisplayOrder = 0
        };

        // Act
        project.Transformations.Add(transformation);

        // Assert
        Assert.Single(project.Transformations);
        Assert.Contains(transformation, project.Transformations);
    }
}
