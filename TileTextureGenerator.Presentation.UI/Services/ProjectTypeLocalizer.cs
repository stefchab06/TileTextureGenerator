using System.Globalization;

namespace TileTextureGenerator.Presentation.UI.Services;

/// <summary>
/// Service to localize project type technical names to user-friendly names.
/// Maps class names (e.g., "FloorTileProject") to localized display names.
/// </summary>
public class ProjectTypeLocalizer
{
    private readonly Dictionary<string, string> _typeToResourceKey = new()
    {
        ["FloorTileProject"] = "ProjectType_FloorTileProject",
        ["WallTileProject"] = "ProjectType_WallTileProject"
    };

    /// <summary>
    /// Gets the localized user-friendly name for a project type.
    /// </summary>
    /// <param name="technicalTypeName">The technical class name (e.g., "FloorTileProject").</param>
    /// <returns>Localized display name (e.g., "Floor Texture" or "Texture de sol").</returns>
    public string GetLocalizedName(string technicalTypeName)
    {
        if (string.IsNullOrWhiteSpace(technicalTypeName))
            return technicalTypeName;

        if (!_typeToResourceKey.TryGetValue(technicalTypeName, out var resourceKey))
            return technicalTypeName; // Fallback to technical name if not found

        return Resources.Strings.AppResources.ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture)
               ?? technicalTypeName;
    }

    /// <summary>
    /// Gets all registered project types with their localized names.
    /// </summary>
    /// <param name="technicalTypeNames">Collection of technical type names.</param>
    /// <returns>Dictionary mapping technical names to localized names.</returns>
    public Dictionary<string, string> GetLocalizedNames(IEnumerable<string> technicalTypeNames)
    {
        return technicalTypeNames.ToDictionary(
            name => name,
            name => GetLocalizedName(name));
    }
}
