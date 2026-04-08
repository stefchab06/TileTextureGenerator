using TileTextureGenerator.Presentation.UI.Resources.Strings;

namespace TileTextureGenerator.Presentation.UI.Services;

/// <summary>
/// Localizes TileShape enum values to user-friendly names.
/// Works with string representation of enum values to avoid Core dependency.
/// </summary>
public class TileShapeLocalizer
{
    /// <summary>
    /// Gets the localized display name for a TileShape value (string).
    /// </summary>
    public string GetLocalizedName(string tileShapeValue)
    {
        var resourceKey = $"TileShape_{tileShapeValue}";
        var localizedName = AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture);
        return localizedName ?? tileShapeValue;
    }

    /// <summary>
    /// Gets all available TileShape values with their localized names.
    /// Returns string values instead of enum to avoid Core dependency.
    /// </summary>
    public IReadOnlyList<TileShapeItem> GetAllTileShapes()
    {
        // Hardcoded list of TileShape values (from Core.Enums.TileShape)
        var shapes = new[] { "Full", "HalfHorizontal", "HalfVertical" };

        return shapes
            .Select(shape => new TileShapeItem
            {
                Value = shape,
                LocalizedName = GetLocalizedName(shape)
            })
            .ToList();
    }
}

/// <summary>
/// Represents a TileShape with its localized name for UI binding.
/// </summary>
public class TileShapeItem
{
    public string Value { get; set; } = string.Empty;
    public string LocalizedName { get; set; } = string.Empty;
}
