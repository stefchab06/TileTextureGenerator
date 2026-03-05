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
    /// Texture image data when Mode = Texture.
    /// Not serialized in JSON - managed separately by the persistence layer.
    /// Null if not applicable.
    /// </summary>
    public byte[]? TextureImage { get; set; }
}
