namespace TileTextureGenerator.Core.Enums;

/// <summary>
/// Type of paper required for printing a transformation output.
/// Different paper types may require separate print jobs or printer settings.
/// </summary>
public enum PaperType
{
    /// <summary>
    /// Standard printing paper (~80-120 g/m²).
    /// Suitable for flat tiles with physical support.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Heavy cardstock (~300 g/m²).
    /// Required for foldable tiles that stand without physical support.
    /// </summary>
    Heavy = 1
}
