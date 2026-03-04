namespace TileTextureGenerator.Core.Enums;

/// <summary>
/// Mode of edge flap filling for transformation borders.
/// Defines how the edge of a tile (flap to be folded) should be rendered.
/// </summary>
public enum EdgeFlapMode
{
    /// <summary>
    /// No flap on this edge (edge remains open).
    /// </summary>
    None,

    /// <summary>
    /// Blank/white flap.
    /// </summary>
    Blank,

    /// <summary>
    /// Solid color fill (color specified in EdgeFlapConfiguration.Color).
    /// </summary>
    Color,

    /// <summary>
    /// Mirror/symmetric copy of the adjacent texture edge.
    /// </summary>
    Symmetric,

    /// <summary>
    /// Custom texture image (path specified in EdgeFlapConfiguration.TextureWorkspacePath).
    /// </summary>
    Texture
}
