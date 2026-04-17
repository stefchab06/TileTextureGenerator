using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Presentation.UI.Helpers;

/// <summary>
/// Helper methods for converting TileShape to cropping polygon coordinates.
/// Polygon points are in proportional coordinates (0-1 range).
/// </summary>
public static class TileShapeHelper
{
    /// <summary>
    /// Gets the cropping polygon for a given TileShape.
    /// Points are in clockwise order starting from top-left.
    /// Coordinates are proportional (0-1 range).
    /// </summary>
    /// <param name="shape">The tile shape to convert.</param>
    /// <returns>Polygon points in proportional coordinates.</returns>
    /// <exception cref="ArgumentException">Thrown when shape is unknown.</exception>
    public static IReadOnlyList<Point> GetCroppingPolygon(TileShape shape)
    {
        return shape switch
        {
            TileShape.Full => new List<Point>
            {
                new Point(0, 0),    // Top-left
                new Point(1, 0),    // Top-right
                new Point(1, 1),    // Bottom-right
                new Point(0, 1)     // Bottom-left
            },

            TileShape.HalfHorizontal => new List<Point>
            {
                new Point(0, 0),      // Top-left
                new Point(1, 0),      // Top-right
                new Point(1, 0.5),    // Bottom-right (half height)
                new Point(0, 0.5)     // Bottom-left (half height)
            },

            TileShape.HalfVertical => new List<Point>
            {
                new Point(0, 0),      // Top-left
                new Point(0.5, 0),    // Top-right (half width)
                new Point(0.5, 1),    // Bottom-right (half width)
                new Point(0, 1)       // Bottom-left
            },

            _ => throw new ArgumentException($"Unknown TileShape: {shape}", nameof(shape))
        };
    }
}
