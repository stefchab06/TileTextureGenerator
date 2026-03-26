namespace TileTextureGenerator.Presentation.UI.Constants;

/// <summary>
/// Defines responsive screen breakpoints for adaptive layouts.
/// Use these constants to determine which layout to display based on screen width.
/// </summary>
public static class ScreenBreakpoints
{
    /// <summary>
    /// Large screens (PC/Desktop). Typically >= 1024px width.
    /// Layout: Horizontal arrangement with all controls on the same row.
    /// </summary>
    public const double Large = 1024;

    /// <summary>
    /// Medium screens (Tablets). Typically >= 600px and &lt; 1024px width.
    /// Layout: Horizontal arrangement, may wrap on smaller tablets.
    /// </summary>
    public const double Medium = 600;

    /// <summary>
    /// Narrow screens (Phones). Typically &lt; 600px width.
    /// Layout: Vertical stacking with each control on its own row.
    /// </summary>
    public const double Narrow = 0;

    /// <summary>
    /// Checks if the given width corresponds to a Large screen.
    /// </summary>
    public static bool IsLarge(double width) => width >= Large;

    /// <summary>
    /// Checks if the given width corresponds to a Medium screen.
    /// </summary>
    public static bool IsMedium(double width) => width >= Medium && width < Large;

    /// <summary>
    /// Checks if the given width corresponds to a Narrow screen.
    /// </summary>
    public static bool IsNarrow(double width) => width < Medium;
}
