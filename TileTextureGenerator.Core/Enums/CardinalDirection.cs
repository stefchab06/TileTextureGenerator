namespace TileTextureGenerator.Core.Enums;

/// <summary>
/// Cardinal directions for orienting tile edges.
/// Used for edge flap configuration and spatial references.
/// </summary>
public enum CardinalDirection
{
    /// <summary>
    /// North (top of the image/tile).
    /// </summary>
    North,

    /// <summary>
    /// South (bottom of the image/tile).
    /// </summary>
    South,

    /// <summary>
    /// East (right side of the image/tile).
    /// </summary>
    East,

    /// <summary>
    /// West (left side of the image/tile).
    /// </summary>
    West
}
