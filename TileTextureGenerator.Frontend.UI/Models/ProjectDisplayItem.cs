using TileTextureGenerator.Adapters.UseCases.Dto;
using TileTextureGenerator.Frontend.UI.Services;

namespace TileTextureGenerator.Frontend.UI.Models;

public sealed class ProjectDisplayItem
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;

    /// <summary>
    /// Display image data for UI binding
    /// </summary>
    public byte[]? DisplayImage { get; init; }

    public ProjectDisplayItem(TextureProjectDto dto)
    {
        Name = dto.Name;
        Type = dto.Type;
        TypeLabel = LocalizationService.Instance.GetString("ProjectType_" + Type);
        Status = dto.Status;
        StatusLabel = LocalizationService.Instance.GetString("ProjectStatus_" + Status);
        DisplayImage = dto.DisplayImage;
    }

    public ProjectDisplayItem() { }

    public ProjectDisplayItem(string name, string type, string typeLabel, string status, string statusLabel, byte[]? displayImage = null)
    {
        Name = name;
        Type = type;
        TypeLabel = typeLabel;
        Status = status;
        StatusLabel = statusLabel;
        DisplayImage = displayImage;
    }

    public TextureProjectDto ToDto()
    {
        return new TextureProjectDto(Name, Type, Status)
        {
            DisplayImage = DisplayImage
        };
    }
}