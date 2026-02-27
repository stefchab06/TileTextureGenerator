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
    /// Relative path to the source image (e.g., "Input\SourceImage.png")
    /// </summary>
    public string? SourceImagePath { get; set; }

    public HorizontalTileTextureProjectEntity(string name)
        : base(name)
    {
        Type = nameof(HorizontalTileTextureProjectEntity);
    }

    /// <summary>
    /// Override to add custom properties specific to horizontal tile projects
    /// </summary>
    protected override void AddCustomPropertiesToJson(JsonObject jsonObject)
    {
        base.AddCustomPropertiesToJson(jsonObject);

        if (!string.IsNullOrEmpty(SourceImagePath))
        {
            jsonObject["SourceImagePath"] = SourceImagePath;
        }
    }

    /// <summary>
    /// Override to load custom properties specific to horizontal tile projects
    /// </summary>
    protected override void LoadCustomPropertiesFromJson(JsonElement rootElement)
    {
        base.LoadCustomPropertiesFromJson(rootElement);

        if (rootElement.TryGetProperty("SourceImagePath", out var sourceImagePath))
        {
            SourceImagePath = sourceImagePath.GetString();
        }
    }
}
