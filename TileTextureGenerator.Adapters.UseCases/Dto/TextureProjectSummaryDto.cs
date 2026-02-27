namespace TileTextureGenerator.Adapters.UseCases.Dto;

/// <summary>
/// DTO for project summaries in use case layer
/// </summary>
public class TextureProjectSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastModifiedDate { get; set; }
    public byte[]? DisplayImage { get; set; }

    public TextureProjectSummaryDto(string name, string type, string status, DateTime lastModifiedDate, byte[]? displayImage = null)
    {
        Name = name;
        Type = type;
        Status = status;
        LastModifiedDate = lastModifiedDate;
        DisplayImage = displayImage;
    }
}
