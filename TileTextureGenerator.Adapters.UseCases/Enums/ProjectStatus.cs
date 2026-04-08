namespace TileTextureGenerator.Adapters.UseCases.Enums;

/// <summary>
/// Project status for UI display.
/// Independent from Core.Enums.ProjectStatus to maintain hexagonal architecture.
/// </summary>
public enum ProjectStatus
{
    Unexisting = 0,
    New = 1,
    Pending = 2,
    Generated = 3,
    Archived = 4
}
