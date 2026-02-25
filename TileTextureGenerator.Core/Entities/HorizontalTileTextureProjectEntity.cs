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

    // Example custom property specific to horizontal tiles
    public int TileWidth { get; set; } = 256;
    public int TileHeight { get; set; } = 256;
    public bool SeamlessMode { get; set; } = false;

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

        jsonObject["TileWidth"] = TileWidth;
        jsonObject["TileHeight"] = TileHeight;
        jsonObject["SeamlessMode"] = SeamlessMode;
    }

    /// <summary>
    /// Override to load custom properties specific to horizontal tile projects
    /// </summary>
    protected override void LoadCustomPropertiesFromJson(JsonElement rootElement)
    {
        base.LoadCustomPropertiesFromJson(rootElement);

        if (rootElement.TryGetProperty("TileWidth", out var tileWidth))
        {
            TileWidth = tileWidth.GetInt32();
        }

        if (rootElement.TryGetProperty("TileHeight", out var tileHeight))
        {
            TileHeight = tileHeight.GetInt32();
        }

        if (rootElement.TryGetProperty("SeamlessMode", out var seamlessMode))
        {
            SeamlessMode = seamlessMode.GetBoolean();
        }
    }

    /// <summary>
    /// Starts the horizontal tile texture generation workflow
    /// </summary>
    protected override async Task<WorkflowAction> OnStartAsync()
    {
        // Return the workflow action to indicate that the UseCase should start horizontal tile generation
        await Task.CompletedTask;
        return WorkflowAction.StartHorizontalTileGeneration;
    }

    /// <summary>
    /// Continues the horizontal tile texture generation workflow
    /// </summary>
    protected override async Task<WorkflowAction> OnContinueAsync()
    {
        // Return the workflow action to indicate that the UseCase should continue horizontal tile generation
        await Task.CompletedTask;
        return WorkflowAction.ContinueHorizontalTileGeneration;
    }
}