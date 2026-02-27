namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Lightweight value object for project summaries in list views
/// Does not contain full entity logic - use TileTextureProjectBase for actual work
/// </summary>
public class TileTextureProjectSummary
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime LastModifiedDate { get; init; }
    public byte[]? DisplayImage { get; init; }

    public TileTextureProjectSummary(string name, string type, string status, DateTime lastModifiedDate, byte[]? displayImage = null)
    {
        Name = name;
        Type = type;
        Status = status;
        LastModifiedDate = lastModifiedDate;
        DisplayImage = displayImage;
    }
}
