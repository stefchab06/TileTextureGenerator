using System.Text.Json.Serialization;
using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Core.Models;

/// <summary>
/// Configuration for a single edge flap (border to be folded).
/// Value object representing the rendering settings for one edge of a tile.
/// </summary>
public class EdgeFlapConfiguration
{
    /// <summary>
    /// Mode determining how this edge flap should be filled.
    /// </summary>
    public EdgeFlapMode Mode { get; set; } = EdgeFlapMode.Blank;

    /// <summary>
    /// Hex color string (e.g., "#808080") when Mode = Color.
    /// Null if not applicable. Will be omitted from JSON when null (global setting).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Texture image data (in-memory) when Mode = Texture.
    /// </summary>
    public ImageData? Texture { get; set; }
}
