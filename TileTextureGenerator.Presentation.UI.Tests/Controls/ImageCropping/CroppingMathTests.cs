using TileTextureGenerator.Presentation.UI.Controls.ImageCropping;

namespace TileTextureGenerator.Presentation.UI.Tests.Controls.ImageCropping;

/// <summary>
/// Tests for CroppingMath helper.
/// Verifies zoom and offset calculations for fit-to-fill behavior.
/// </summary>
public class CroppingMathTests
{
    [Fact]
    public void CalculateFitToFill_WithSquareImageAndSquarePolygon_ReturnsZoom1AndCenteredOffset()
    {
        // Arrange
        var polygon = new List<Point>
        {
            new Point(0, 0),
            new Point(100, 0),
            new Point(100, 100),
            new Point(0, 100)
        };
        var imageSize = new SizeF(100, 100);
        var canvasSize = new SizeF(400, 400); // Canvas larger than needed

        // Act
        var result = CroppingMath.CalculateFitToFill(imageSize, canvasSize, polygon);

        // Assert
        Assert.Equal(1.0, result.Zoom, precision: 2);
        Assert.Equal(0.0, result.Offset.X, precision: 2);
        Assert.Equal(0.0, result.Offset.Y, precision: 2);
    }

    [Fact]
    public void CalculateFitToFill_WithWideImageAndSquarePolygon_ScalesAndCentersHorizontally()
    {
        // Arrange: Image 200x100, Polygon 100x100
        var polygon = new List<Point>
        {
            new Point(0, 0),
            new Point(100, 0),
            new Point(100, 100),
            new Point(0, 100)
        };
        var imageSize = new SizeF(200, 100); // Wide image
        var canvasSize = new SizeF(400, 400);

        // Act
        var result = CroppingMath.CalculateFitToFill(imageSize, canvasSize, polygon);

        // Assert
        // Zoom should be CH/PH = 100/100 = 1.0 (height fills, width overflows)
        Assert.Equal(1.0, result.Zoom, precision: 2);
        
        // Scaled width = 200 * 1.0 = 200
        // Offset X = 0 + (100 - 200) / 2 = -50 (image overflows left/right)
        Assert.Equal(-50.0, result.Offset.X, precision: 2);
        Assert.Equal(0.0, result.Offset.Y, precision: 2);
    }

    [Fact]
    public void CalculateFitToFill_WithTallImageAndSquarePolygon_ScalesAndCentersVertically()
    {
        // Arrange: Image 100x200, Polygon 100x100
        var polygon = new List<Point>
        {
            new Point(0, 0),
            new Point(100, 0),
            new Point(100, 100),
            new Point(0, 100)
        };
        var imageSize = new SizeF(100, 200); // Tall image
        var canvasSize = new SizeF(400, 400);

        // Act
        var result = CroppingMath.CalculateFitToFill(imageSize, canvasSize, polygon);

        // Assert
        // Zoom should be CW/PW = 100/100 = 1.0 (width fills, height overflows)
        Assert.Equal(1.0, result.Zoom, precision: 2);
        
        // Scaled height = 200 * 1.0 = 200
        // Offset Y = 0 + (100 - 200) / 2 = -50 (image overflows top/bottom)
        Assert.Equal(0.0, result.Offset.X, precision: 2);
        Assert.Equal(-50.0, result.Offset.Y, precision: 2);
    }

    [Fact]
    public void CalculateFitToFill_WithSmallImageAndLargePolygon_UpscalesImage()
    {
        // Arrange: Image 50x50, Polygon 100x100
        var polygon = new List<Point>
        {
            new Point(0, 0),
            new Point(100, 0),
            new Point(100, 100),
            new Point(0, 100)
        };
        var imageSize = new SizeF(50, 50);
        var canvasSize = new SizeF(400, 400);

        // Act
        var result = CroppingMath.CalculateFitToFill(imageSize, canvasSize, polygon);

        // Assert
        // Zoom should be Max(100/50, 100/50) = 2.0
        Assert.Equal(2.0, result.Zoom, precision: 2);
        
        // Scaled = 50 * 2 = 100
        // Offset = 0 (perfect fit)
        Assert.Equal(0.0, result.Offset.X, precision: 2);
        Assert.Equal(0.0, result.Offset.Y, precision: 2);
    }

    [Fact]
    public void CalculateFitToFill_WithPolygonNotAtOrigin_OffsetsCorrectly()
    {
        // Arrange: Polygon offset to (50, 50), size 100x100
        var polygon = new List<Point>
        {
            new Point(50, 50),
            new Point(150, 50),
            new Point(150, 150),
            new Point(50, 150)
        };
        var imageSize = new SizeF(100, 100);
        var canvasSize = new SizeF(400, 400);

        // Act
        var result = CroppingMath.CalculateFitToFill(imageSize, canvasSize, polygon);

        // Assert
        Assert.Equal(1.0, result.Zoom, precision: 2);
        
        // MinX = 50, MinY = 50, so offset should be (50, 50)
        Assert.Equal(50.0, result.Offset.X, precision: 2);
        Assert.Equal(50.0, result.Offset.Y, precision: 2);
    }

    [Fact]
    public void CalculateFitToFill_WithRectangularPolygon_HandlesAspectRatio()
    {
        // Arrange: Wide polygon 200x100, square image 100x100
        var polygon = new List<Point>
        {
            new Point(0, 0),
            new Point(200, 0),
            new Point(200, 100),
            new Point(0, 100)
        };
        var imageSize = new SizeF(100, 100);
        var canvasSize = new SizeF(400, 400);

        // Act
        var result = CroppingMath.CalculateFitToFill(imageSize, canvasSize, polygon);

        // Assert
        // Zoom should be Max(100/100, 200/100) = Max(1.0, 2.0) = 2.0
        Assert.Equal(2.0, result.Zoom, precision: 2);
        
        // Scaled = 100 * 2 = 200 (both dimensions)
        // Offset X = 0 + (200 - 200) / 2 = 0
        // Offset Y = 0 + (100 - 200) / 2 = -50
        Assert.Equal(0.0, result.Offset.X, precision: 2);
        Assert.Equal(-50.0, result.Offset.Y, precision: 2);
    }

    [Fact]
    public void CalculateFitToFill_WithComplexPolygon_UsesCircumscribedRectangle()
    {
        // Arrange: Irregular polygon, bounding box is (10, 20) to (110, 120)
        var polygon = new List<Point>
        {
            new Point(10, 20),
            new Point(60, 10),  // Top point (ignored for height, MinY=10)
            new Point(110, 70),
            new Point(60, 120),
            new Point(30, 80)
        };
        var imageSize = new SizeF(100, 100);
        var canvasSize = new SizeF(400, 400);

        // Act
        var result = CroppingMath.CalculateFitToFill(imageSize, canvasSize, polygon);

        // Assert
        // Bounding box: MinX=10, MaxX=110, MinY=10, MaxY=120
        // CW = 100, CH = 110
        // Zoom = Max(110/100, 100/100) = 1.1
        Assert.Equal(1.1, result.Zoom, precision: 2);
        
        // Scaled = 100 * 1.1 = 110
        // Offset X = 10 + (100 - 110) / 2 = 5
        // Offset Y = 10 + (110 - 110) / 2 = 10
        Assert.Equal(5.0, result.Offset.X, precision: 2);
        Assert.Equal(10.0, result.Offset.Y, precision: 2);
    }
}
