using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Core.Transformations;

/// <summary>
/// Configuration for a single edge flap (border to be folded).
/// </summary>
public class EdgeFlapConfiguration
{
    /// <summary>
    /// Mode determining how this edge flap should be filled.
    /// </summary>
    public EdgeFlapMode Mode { get; set; } = EdgeFlapMode.Blank;

    /// <summary>
    /// Hex color string (e.g., "#808080") when Mode = Color.
    /// Null if not applicable.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Relative path to texture image in Workspace/ directory when Mode = Texture.
    /// Format: "guid.png"
    /// Null if not applicable.
    /// </summary>
    public string? TextureWorkspacePath { get; set; }
}
