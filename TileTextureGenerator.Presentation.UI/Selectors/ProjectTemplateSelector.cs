using TileTextureGenerator.Presentation.UI.Templates;
using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Selectors;

/// <summary>
/// Holds template and ViewModel type information for a concrete project type.
/// </summary>
/// <param name="Template">The DataTemplate for rendering the project UI.</param>
/// <param name="ViewModelType">The ViewModel type associated with this project template.</param>
public record ProjectTemplateInfo(DataTemplate Template, Type ViewModelType);

/// <summary>
/// Selects the appropriate DataTemplate and ViewModel based on the concrete project type name.
/// Centralized registration: all project templates are registered in the constructor.
/// 
/// To add a new project type:
/// 1. Create the Template class (e.g., WallTileTemplate.xaml + code-behind)
/// 2. Create the ViewModel class (e.g., WallTileProjectViewModel.cs)
/// 3. Add one line in the constructor:
///    RegisterTemplate("WallTileProject", typeof(WallTileTemplate), typeof(WallTileProjectViewModel));
/// That's it! No XAML modification needed.
/// </summary>
public class ProjectTemplateSelector : DataTemplateSelector
{
    private readonly Dictionary<string, ProjectTemplateInfo> _projectTemplates = new();

    /// <summary>
    /// Constructor: Register all project templates here.
    /// </summary>
    public ProjectTemplateSelector()
    {
        // Register FloorTileProject
        RegisterTemplate("FloorTileProject", typeof(FloorTileTemplate), typeof(FloorTileProjectViewModel));

        // To add WallTileProject, uncomment:
        // RegisterTemplate("WallTileProject", typeof(WallTileTemplate), typeof(WallTileProjectViewModel));
    }

    /// <summary>
    /// Registers a project template with its associated ViewModel type.
    /// </summary>
    /// <param name="projectTypeName">The concrete project type name (e.g., "FloorTileProject").</param>
    /// <param name="templateType">The template type (e.g., typeof(FloorTileTemplate)).</param>
    /// <param name="viewModelType">The ViewModel type (e.g., typeof(FloorTileProjectViewModel)).</param>
    private void RegisterTemplate(string projectTypeName, Type templateType, Type viewModelType)
    {
        var dataTemplate = new DataTemplate(templateType);
        _projectTemplates[projectTypeName] = new ProjectTemplateInfo(dataTemplate, viewModelType);
    }

    /// <summary>
    /// Gets the list of registered project type names.
    /// Useful for testing to verify all concrete project types are registered.
    /// </summary>
    public IReadOnlyCollection<string> RegisteredProjectTypes => _projectTemplates.Keys;

    /// <summary>
    /// Gets the complete template information (DataTemplate + ViewModel type) for a project type.
    /// </summary>
    /// <param name="projectTypeName">The concrete project type name.</param>
    /// <returns>Template info if found, null otherwise.</returns>
    public ProjectTemplateInfo? GetTemplateInfo(string projectTypeName)
    {
        if (string.IsNullOrEmpty(projectTypeName))
            return null;

        return _projectTemplates.TryGetValue(projectTypeName, out var info) ? info : null;
    }

    /// <summary>
    /// Selects the appropriate template based on the ConcreteTypeName.
    /// </summary>
    public DataTemplate? SelectTemplateByTypeName(string concreteTypeName)
    {
        return GetTemplateInfo(concreteTypeName)?.Template;
    }

    /// <summary>
    /// Gets ViewModel type for a given project type name.
    /// </summary>
    public Type? GetViewModelType(string concreteTypeName)
    {
        return GetTemplateInfo(concreteTypeName)?.ViewModelType;
    }

    /// <summary>
    /// Gets the template mappings for testing purposes (legacy compatibility).
    /// </summary>
    public IReadOnlyDictionary<string, DataTemplate?> GetTemplateMappings()
    {
        return _projectTemplates.ToDictionary(
            kvp => kvp.Key,
            kvp => (DataTemplate?)kvp.Value.Template
        );
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
