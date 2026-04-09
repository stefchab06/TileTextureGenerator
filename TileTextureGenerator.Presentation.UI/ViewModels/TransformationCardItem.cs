namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// Item for transformation card display (icon + names + id).
/// </summary>
public class TransformationCardItem
{
    public Guid Id { get; set; }
    public string TechnicalName { get; set; } = string.Empty;
    public string LocalizedName { get; set; } = string.Empty;
    public byte[] Icon { get; set; } = Array.Empty<byte>();
}
