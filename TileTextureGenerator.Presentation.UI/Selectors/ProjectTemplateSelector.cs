using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Selectors;

/// <summary>
/// Selects the appropriate DataTemplate based on the concrete project type name.
/// Exposes template mappings for testing purposes.
/// Automatically scans DataTemplate properties to build mappings (convention-based).
/// Also maps TypeName to ViewModel Type for generic instantiation.
/// </summary>
public class ProjectTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Template for FloorTileProject.
    /// Convention: Property name "FloorTileTemplate" maps to type "FloorTileProject" and ViewModel "FloorTileProjectViewModel".
    /// </summary>
    public DataTemplate? FloorTileTemplate { get; set; }

    // To add a new project type:
    // 1. Add a property: public DataTemplate? WallTileTemplate { get; set; }
    // 2. Configure it in XAML Resources
    // That's it! Mapping is automatic via reflection and naming convention.

    /// <summary>
    /// Gets the template mappings for testing purposes.
    /// Automatically scans all DataTemplate properties and builds mappings.
    /// Convention: "XxxTemplate" property name → "XxxProject" type name.
    /// </summary>
    public IReadOnlyDictionary<string, DataTemplate?> GetTemplateMappings()
    {
        var mappings = new Dictionary<string, DataTemplate?>();

        // Scan all properties that are DataTemplate
        var properties = GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(DataTemplate) && p.CanRead);

        foreach (var prop in properties)
        {
            // Extract type name from property name (e.g., "FloorTileTemplate" -> "FloorTileProject")
            var propName = prop.Name;
            if (propName.EndsWith("Template"))
            {
                var typeName = propName.Substring(0, propName.Length - "Template".Length) + "Project";
                var template = (DataTemplate?)prop.GetValue(this);
                mappings[typeName] = template;
            }
        }

        return mappings;
    }

    /// <summary>
    /// Gets ViewModel type for a given project type name.
    /// Convention: "FloorTileProject" → "FloorTileProjectViewModel"
    /// </summary>
    public Type? GetViewModelType(string concreteTypeName)
    {
        if (string.IsNullOrEmpty(concreteTypeName))
            return null;

        var viewModelTypeName = $"TileTextureGenerator.Presentation.UI.ViewModels.{concreteTypeName}ViewModel";
        return Type.GetType(viewModelTypeName);
    }

    /// <summary>
    /// Selects the appropriate template based on the ConcreteTypeName.
    /// </summary>
    public DataTemplate? SelectTemplateByTypeName(string concreteTypeName)
    {
        if (string.IsNullOrEmpty(concreteTypeName))
            return null;

        var mappings = GetTemplateMappings();
        return mappings.TryGetValue(concreteTypeName, out var template) ? template : null;
    }

    /// <summary>
    /// Legacy method for DataTemplateSelector (not used in new architecture).
    /// </summary>
    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        // Not used anymore - template selection is done via ConcreteTypeName
        return null;
    }
}
