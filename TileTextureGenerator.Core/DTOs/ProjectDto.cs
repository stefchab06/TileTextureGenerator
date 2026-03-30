using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.DTOs;

/// <summary>
/// Data transfer object representing a project summary for list views.
/// Contains essential information without full entity logic.
/// </summary>
public sealed class ProjectDto
{
    /// <summary>
    /// Name of the project (unique identifier).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Type of the project (e.g., HorizontalTileTextureProjectEntity).
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the project.
    /// </summary>
    public ProjectStatus Status { get; init; }

    /// <summary>
    /// Display image for the project (PNG, 256x256).
    /// </summary>
    public ImageData? DisplayImage { get; init; }

    /// <summary>
    /// Last modification date (UTC).
    /// </summary>
    public DateTime LastModifiedDate { get; init; }

    /// <summary>
    /// Actions available for this project based on its current status.
    /// </summary>
    public ProjectActions AvailableActions { get; init; }

    public ProjectDto(
        string name,
        string type,
        ProjectStatus status,
        DateTime lastModifiedDate,
        ImageData? displayImage = null,
        ProjectActions availableActions = ProjectActions.None)
    {
        Name = name;
        Type = type;
        Status = status;
        LastModifiedDate = lastModifiedDate;
        DisplayImage = displayImage;
        AvailableActions = availableActions;
    }
}
