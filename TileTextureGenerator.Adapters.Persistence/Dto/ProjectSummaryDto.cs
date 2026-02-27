namespace TileTextureGenerator.Adapters.Persistence.Dto;

/// <summary>
/// Lightweight DTO for project list display
/// Contains only essential data without full entity instantiation
/// </summary>
public class ProjectSummaryDto
{
    public string ProjectName { get; init; } = string.Empty;
    public string ProjectType { get; init; } = string.Empty;
    public string ProjectStatus { get; init; } = string.Empty;
    public DateTime LastModifiedDate { get; init; }
    public byte[]? DisplayImage { get; init; }

    public ProjectSummaryDto(string projectName, string projectType, string projectStatus, DateTime lastModifiedDate)
    {
        ProjectName = projectName;
        ProjectType = projectType;
        ProjectStatus = projectStatus;
        LastModifiedDate = lastModifiedDate;
    }
}
