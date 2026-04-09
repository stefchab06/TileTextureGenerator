namespace TileTextureGenerator.Adapters.UseCases;

/// <summary>
/// DTO for transformation list display in the UI (decoupled from Core).
/// Contains essential information for rendering transformation cards.
/// </summary>
public record TransformationListItemDto(
    Guid Id,
    string TypeName,
    byte[] Icon
);
