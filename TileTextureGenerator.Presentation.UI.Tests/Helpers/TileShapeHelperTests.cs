using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Presentation.UI.Helpers;

namespace TileTextureGenerator.Presentation.UI.Tests.Helpers;

/// <summary>
/// Tests for TileShapeHelper.
/// Verifies conversion from TileShape enum to cropping polygon points.
/// </summary>
public class TileShapeHelperTests
{
    [Fact]
    public void GetCroppingPolygon_ForFullShape_ReturnsSquarePolygon()
    {
        // Arrange
        var tileShape = TileShape.Full;

        // Act
        var polygon = TileShapeHelper.GetCroppingPolygon(tileShape);

        // Assert
        Assert.NotNull(polygon);
        Assert.Equal(4, polygon.Count); // Rectangle has 4 points

        // Verify it's a square (proportions 1:1)
        Assert.Equal(new Point(0, 0), polygon[0]);
        Assert.Equal(new Point(1, 0), polygon[1]);
        Assert.Equal(new Point(1, 1), polygon[2]);
        Assert.Equal(new Point(0, 1), polygon[3]);
    }

    [Fact]
    public void GetCroppingPolygon_ForHalfHorizontalShape_ReturnsHorizontalRectangle()
    {
        // Arrange
        var tileShape = TileShape.HalfHorizontal;

        // Act
        var polygon = TileShapeHelper.GetCroppingPolygon(tileShape);

        // Assert
        Assert.NotNull(polygon);
        Assert.Equal(4, polygon.Count);

        // Verify it's a horizontal rectangle (width=1, height=0.5)
        Assert.Equal(new Point(0, 0), polygon[0]);
        Assert.Equal(new Point(1, 0), polygon[1]);
        Assert.Equal(new Point(1, 0.5), polygon[2]);
        Assert.Equal(new Point(0, 0.5), polygon[3]);
    }

    [Fact]
    public void GetCroppingPolygon_ForHalfVerticalShape_ReturnsVerticalRectangle()
    {
        // Arrange
        var tileShape = TileShape.HalfVertical;

        // Act
        var polygon = TileShapeHelper.GetCroppingPolygon(tileShape);

        // Assert
        Assert.NotNull(polygon);
        Assert.Equal(4, polygon.Count);

        // Verify it's a vertical rectangle (width=0.5, height=1)
        Assert.Equal(new Point(0, 0), polygon[0]);
        Assert.Equal(new Point(0.5, 0), polygon[1]);
        Assert.Equal(new Point(0.5, 1), polygon[2]);
        Assert.Equal(new Point(0, 1), polygon[3]);
    }

    [Fact]
    public void GetCroppingPolygon_WithInvalidTileShape_ThrowsArgumentException()
    {
        // Arrange
        var invalidShape = (TileShape)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            TileShapeHelper.GetCroppingPolygon(invalidShape));
        
        Assert.Contains("Unknown TileShape", exception.Message);
    }

    [Fact]
    public void GetCroppingPolygon_ReturnsReadOnlyCollection()
    {
        // Arrange
        var tileShape = TileShape.Full;

        // Act
        var polygon = TileShapeHelper.GetCroppingPolygon(tileShape);

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Point>>(polygon);
    }

    [Fact]
    public void GetCroppingPolygon_PolygonPointsAreInClockwiseOrder()
    {
        // Arrange & Act
        var fullPolygon = TileShapeHelper.GetCroppingPolygon(TileShape.Full);

        // Assert - Verify clockwise order (top-left → top-right → bottom-right → bottom-left)
        // For a square starting at (0,0), clockwise means:
        // (0,0) → (1,0) → (1,1) → (0,1)
        Assert.True(fullPolygon[0].X == 0 && fullPolygon[0].Y == 0); // Top-left
        Assert.True(fullPolygon[1].X > fullPolygon[0].X); // Moving right
        Assert.True(fullPolygon[2].Y > fullPolygon[1].Y); // Moving down
        Assert.True(fullPolygon[3].X < fullPolygon[2].X); // Moving left
    }
}
