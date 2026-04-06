using TileTextureGenerator.Presentation.UI.Resources.Strings;

namespace TileTextureGenerator.Presentation.UI.Services;

/// <summary>
/// Localizes transformation type names from technical names to user-friendly localized names.
/// </summary>
public class TransformationTypeLocalizer
{
    /// <summary>
    /// Gets the localized display name for a transformation type.
    /// </summary>
    /// <param name="technicalName">Technical transformation type name (e.g., "HorizontalFloorTransformation")</param>
    /// <returns>Localized user-friendly name (e.g., "Floor Texture" or "Texture de sol")</returns>
    public string GetLocalizedName(string technicalName)
    {
        var resourceKey = $"TransformationType_{technicalName}";
        
        // Try to get localized resource
        var localizedName = AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture);
        
        // Fallback to technical name if resource not found
        return localizedName ?? technicalName;
    }
}
