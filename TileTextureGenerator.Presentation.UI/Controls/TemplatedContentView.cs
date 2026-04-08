using TileTextureGenerator.Presentation.UI.Selectors;
using TileTextureGenerator.Presentation.UI.Templates;

namespace TileTextureGenerator.Presentation.UI.Controls;

/// <summary>
/// ContentView that dynamically selects and instantiates a DataTemplate based on a type name.
/// Used with ProjectTemplateSelector to display the correct template for each project type.
/// If no specific template exists, displays PlaceholderTemplate automatically.
/// </summary>
public class TemplatedContentView : ContentView
{
    public static readonly BindableProperty TemplateSelectorProperty =
        BindableProperty.Create(
            nameof(TemplateSelector),
            typeof(ProjectTemplateSelector),
            typeof(TemplatedContentView),
            propertyChanged: OnTemplateSelectorChanged);

    public static readonly BindableProperty TypeNameProperty =
        BindableProperty.Create(
            nameof(TypeName),
            typeof(string),
            typeof(TemplatedContentView),
            propertyChanged: OnTypeNameChanged);

    public static readonly BindableProperty TemplateDataProperty =
        BindableProperty.Create(
            nameof(TemplateData),
            typeof(object),
            typeof(TemplatedContentView),
            propertyChanged: OnTemplateDataChanged);

    /// <summary>
    /// The template selector used to choose the appropriate template.
    /// </summary>
    public ProjectTemplateSelector? TemplateSelector
    {
        get => (ProjectTemplateSelector?)GetValue(TemplateSelectorProperty);
        set => SetValue(TemplateSelectorProperty, value);
    }

    /// <summary>
    /// The type name used to select the template (e.g., "FloorTileProject").
    /// </summary>
    public string? TypeName
    {
        get => (string?)GetValue(TypeNameProperty);
        set => SetValue(TypeNameProperty, value);
    }

    /// <summary>
    /// The data object to bind to the selected template.
    /// </summary>
    public object? TemplateData
    {
        get => GetValue(TemplateDataProperty);
        set => SetValue(TemplateDataProperty, value);
    }

    private static void OnTemplateSelectorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemplatedContentView control)
            control.UpdateContent();
    }

    private static void OnTypeNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemplatedContentView control)
            control.UpdateContent();
    }

    private static void OnTemplateDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemplatedContentView control)
            control.UpdateContent();
    }

    /// <summary>
    /// Updates the content by selecting and instantiating the appropriate template.
    /// Falls back to PlaceholderTemplate if no specific template is found.
    /// </summary>
    private void UpdateContent()
    {
        // Clear existing content first
        Content = null;

        if (TemplateSelector == null || string.IsNullOrEmpty(TypeName))
            return;

        // Select template by type name
        var template = TemplateSelector.SelectTemplateByTypeName(TypeName);

        if (template != null)
        {
            // Instantiate specific template and set binding context
            var view = (View)template.CreateContent();
            view.BindingContext = TemplateData;
            Content = view;
        }
        else
        {
            // No specific template found: use placeholder
            Content = new PlaceholderTemplate();
        }
    }
}
