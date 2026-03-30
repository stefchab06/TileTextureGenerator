using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Adapters.UseCases;

/// <summary>
/// DTO for project list display in the UI (decoupled from Core).
/// </summary>
public record ProjectListItemDto(
    string Name,
    string Type,
    ProjectStatus Status,
    byte[]? DisplayImage,
    bool CanLoad,
    bool CanGenerate,
    bool CanArchive,
    bool CanDelete
);
