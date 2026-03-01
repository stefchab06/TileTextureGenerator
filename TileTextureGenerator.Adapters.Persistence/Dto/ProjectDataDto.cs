using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Adapters.Persistence.Dto;

public class ProjectDataDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string ProjectStatus { get; set; } = string.Empty;
    public string ProjectDataJson { get; set; } = string.Empty;
    public byte[]? DisplayImage { get; set; }

    /// <summary>
    /// Source image data (from Core domain)
    /// </summary>
    public byte[]? SourceImage { get; set; }

    /// <summary>
    /// Relative path to display image file (persistence detail)
    /// </summary>
    public string? DisplayImageFile { get; set; }

    /// <summary>
    /// Relative path to source image file (persistence detail)
    /// </summary>
    public string? SourceImageFile { get; set; }

    /// <summary>
    /// Last modification date
    /// </summary>
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    public ProjectDataDto(string projectName, string projectType, string projectStatus, string projectDataJson = "")
    {
        ProjectName = projectName;
        ProjectType = projectType;
        ProjectStatus = projectStatus;
        ProjectDataJson = projectDataJson;
    }

    public ProjectDataDto(TileTextureProjectBase coreProject)
    {
        ProjectName = coreProject.Name;
        ProjectType = coreProject.Type;
        ProjectStatus = coreProject.Status.ToString();
        ProjectDataJson = coreProject.ToJson();
        DisplayImage = coreProject.DisplayImage;
        LastModifiedDate = coreProject.LastModifiedDate;

        // Extract SourceImage if it's a HorizontalTileTextureProjectEntity
        if (coreProject is HorizontalTileTextureProjectEntity horizontalProject)
        {
            SourceImage = horizontalProject.SourceImage;
        }
    }
}
