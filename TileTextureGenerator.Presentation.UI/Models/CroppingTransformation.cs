namespace TileTextureGenerator.Presentation.UI.Models;

/// <summary>
/// Represents the transformation state of an image being cropped.
/// All transformations are relative to the initial "fit-to-fill" state.
/// </summary>
/// <param name="Zoom">Zoom factor (1.0 = fit-to-fill, >1.0 = zoomed in).</param>
/// <param name="PanX">Horizontal pan offset in pixels.</param>
/// <param name="PanY">Vertical pan offset in pixels.</param>
/// <param name="Rotation">Rotation angle in degrees (0-360).</param>
public record CroppingTransformation(
    double Zoom = 1.0,
    double PanX = 0.0,
    double PanY = 0.0,
    double Rotation = 0.0
)
{
    /// <summary>
    /// Identity transformation (no changes).
    /// </summary>
    public static CroppingTransformation Identity => new();

    /// <summary>
    /// Creates a transformation with only pan applied.
    /// </summary>
    public static CroppingTransformation Pan(double x, double y) => new(PanX: x, PanY: y);

    /// <summary>
    /// Creates a transformation with only zoom applied.
    /// </summary>
    public static CroppingTransformation ZoomOnly(double zoom) => new(Zoom: zoom);

    /// <summary>
    /// Applies a pan delta to the current transformation.
    /// </summary>
    public CroppingTransformation WithPanDelta(double deltaX, double deltaY) =>
        this with { PanX = PanX + deltaX, PanY = PanY + deltaY };

    /// <summary>
    /// Applies a zoom factor to the current transformation.
    /// </summary>
    public CroppingTransformation WithZoom(double zoom) =>
        this with { Zoom = zoom };

    /// <summary>
    /// Applies a rotation to the current transformation.
    /// </summary>
    public CroppingTransformation WithRotation(double rotation) =>
        this with { Rotation = rotation };
}
