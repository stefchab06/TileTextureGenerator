using System.Text.Json.Serialization;
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
    /// Null if not applicable. Will be omitted from JSON when null (global setting).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Relative path to texture image file (e.g., "Workspace\guid.png") when Mode = Texture.
    /// Null if not applicable. Will be omitted from JSON when null (global setting).
    /// </summary>
    public string? Texture { get; set; }

    /// <summary>
    /// Texture image data (in-memory) when Mode = Texture.
    /// Not serialized in JSON - only the path (Texture property) is persisted.
    /// Used at runtime to hold the actual image bytes.
    /// </summary>
    [JsonIgnore]
    public byte[]? TextureImage { get; set; }
}
