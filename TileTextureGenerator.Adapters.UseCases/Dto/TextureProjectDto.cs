using System;
using System.Collections.Generic;
using System.Text;

namespace TileTextureGenerator.Adapters.UseCases.Dto;

public class TextureProjectDto
{
    public string Name { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Display image data in bytes (null if no image)
    /// </summary>
    public byte[]? DisplayImage { get; set; }

    /// <summary>
    /// Last modification date (UTC)
    /// </summary>
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    public TextureProjectDto()
    {
        Name = string.Empty;
        Type = string.Empty;
        Status = string.Empty;
    }

    public TextureProjectDto(string name, string type, string status)
    {
        Name = name;
        Type = type;
        Status = status;
    }
}
