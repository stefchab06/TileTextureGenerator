using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Registries;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace TileTextureGenerator.Core.Entities;

public sealed class HorizontalTileTextureProjectEntity : TileTextureProjectBase
{
    // NOTE: Auto-registration temporarily disabled during refactoring
    // This class will be replaced by FloorTileProject
    /*
    static HorizontalTileTextureProjectEntity()
    {
        TextureProjectRegistry.Register(
            key: nameof(HorizontalTileTextureProjectEntity),
            factory: (name) => new HorizontalTileTextureProjectEntity(name)
        );
    }
    */

    /// <summary>
    /// Source image data (PNG format, full resolution)
    /// Core domain uses byte[] - persistence details handled in adapters
    /// </summary>
    public byte[]? SourceImage { get; set; }

    /// <summary>
    /// Shape of the tile (Full, HalfHorizontal, HalfVertical)
    /// </summary>
    public TileShape TileShape { get; set; } = TileShape.Full;

    /// <summary>
    /// List of transformations configured for this project.
    /// Each transformation will generate one output image.
    /// </summary>
    public List<TransformationEntity> Transformations { get; set; } = new();

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

        // Serialize transformations using the custom converter
        if (Transformations.Count > 0)
        {
            var transformationsJson = JsonSerializer.Serialize(
                Transformations,
                Extensions.JsonOptionsExtensions.GetDefaultOptions());
            jsonObject["Transformations"] = JsonNode.Parse(transformationsJson);
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

        // Deserialize transformations using the custom converter
        if (rootElement.TryGetProperty("Transformations", out var transformationsElement))
        {
            try
            {
                var transformations = JsonSerializer.Deserialize<List<TransformationEntity>>(
                    transformationsElement.GetRawText(),
                    Extensions.JsonOptionsExtensions.GetDefaultOptions());

                if (transformations != null)
                {
                    Transformations = transformations;
                }
            }
            catch
            {
                // If deserialization fails, keep empty list
                Transformations = new List<TransformationEntity>();
            }
        }

        // SourceImage will be loaded by the persistence layer
    }
}
