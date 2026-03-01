using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Registries;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace TileTextureGenerator.Core.Entities;

public sealed class HorizontalTileTextureProjectEntity : TileTextureProjectBase
{
    // Auto-registration: identifier = class name
    static HorizontalTileTextureProjectEntity()
    {
        TextureProjectRegistry.Register(
            key: nameof(HorizontalTileTextureProjectEntity),
            factory: (name) => new HorizontalTileTextureProjectEntity(name)
        );
    }

    /// <summary>
    /// Source image data (PNG format, full resolution)
    /// Core domain uses byte[] - persistence details handled in adapters
    /// </summary>
    public byte[]? SourceImage { get; set; }

    /// <summary>
    /// Shape of the tile (Full, HalfHorizontal, HalfVertical)
    /// </summary>
    public TileShape TileShape { get; set; } = TileShape.Full;

    public HorizontalTileTextureProjectEntity(string name)
        : base(name)
    {
        Type = nameof(HorizontalTileTextureProjectEntity);
    }

    /// <summary>
    /// Override to add custom properties specific to horizontal tile projects
    /// Note: SourceImage is NOT serialized here (too large) - handled by persistence layer
    /// </summary>
    protected override void AddCustomPropertiesToJson(JsonObject jsonObject)
    {
        base.AddCustomPropertiesToJson(jsonObject);

        jsonObject["TileShape"] = TileShape.ToString();

        // Store a flag indicating if source image exists
        if (SourceImage != null && SourceImage.Length > 0)
        {
            jsonObject["HasSourceImage"] = true;
        }
    }

    /// <summary>
    /// Override to load custom properties specific to horizontal tile projects
    /// Note: SourceImage is loaded separately by persistence layer
    /// </summary>
    protected override void LoadCustomPropertiesFromJson(JsonElement rootElement)
    {
        base.LoadCustomPropertiesFromJson(rootElement);

        if (rootElement.TryGetProperty("TileShape", out var tileShapeElement))
        {
            if (Enum.TryParse<TileShape>(tileShapeElement.GetString(), out var tileShape))
            {
                TileShape = tileShape;
            }
        }

        // SourceImage will be loaded by the persistence layer
    }
}
