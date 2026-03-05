using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Port for navigation operations.
/// Allows workflows to request navigation without knowing UI details.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigate to transformations management view for a project.
    /// </summary>
    Task NavigateToTransformationsManagementAsync(HorizontalTileTextureProjectEntity project);

    /// <summary>
    /// Navigate back.
    /// </summary>
    Task NavigateBackAsync();
}
