namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// Item for transformation card display (icon + names + id).
/// </summary>
public class TransformationCardItem
{
    public Guid Id { get; set; }
    public string TechnicalName { get; set; } = string.Empty;
    public string LocalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Icon as ImageSource (converted from byte[] for XAML binding).
    /// </summary>
    public ImageSource? IconSource { get; set; }
}
