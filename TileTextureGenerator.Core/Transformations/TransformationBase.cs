using System.Reflection;
using TileTextureGenerator.Core.Attributes;

namespace TileTextureGenerator.Core.Transformations;

/// <summary>
/// Base class for all texture transformations.
/// Each concrete transformation type should inherit from this and implement the abstract methods.
/// </summary>
public abstract class TransformationBase
{
    /// <summary>
    /// Standard border width for blank flaps (in pixels).
    /// </summary>
    protected const float BlankBorderWidth = 2f;

    /// <summary>
    /// Maximum tile dimension in inches (for standard tiles).
    /// Used to calculate DPI from image dimensions.
    /// </summary>
    protected const double MaxTileDimensionInInches = 2.0;

    /// <summary>
    /// Unique identifier for this transformation instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Edge flap configurations for all four sides of the tile.
    /// Defines how borders (to be folded) should be rendered.
    /// </summary>
    [TransformationProperty]
    public EdgeFlapsCollection EdgeFlaps { get; set; } = new();

    /// <summary>
    /// Gets the localized display name for this transformation instance.
    /// Should include type and key property values.
    /// Example: "Plan Incliné (Nord 0.5")" or "Inclined Plane (North 0.5")"
    /// </summary>
    public abstract string GetDisplayName();

    /// <summary>
    /// Gets a filesystem-safe filename for the output image.
    /// Must contain only valid filename characters (no spaces, special chars).
    /// Should be descriptive and unique within a project.
    /// Example: "inclined_north_0_50" (without extension)
    /// </summary>
    public abstract string GetSafeFileName();

    /// <summary>
    /// Gets the path to the icon resource representing this transformation type.
    /// This should be an embedded resource in the UI project.
    /// Example: "Resources/Images/Transformations/inclined_plane.png"
    /// </summary>
    public abstract string GetIconResourcePath();

    /// <summary>
    /// Executes the transformation and generates the output image.
    /// </summary>
    /// <param name="context">Project context with source image and configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the generated image(s) and status</returns>
    public abstract Task<TransformationResult> ExecuteAsync(
        ProjectContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Serializes all properties marked with [TransformationProperty] to a dictionary.
    /// Complex objects are pre-serialized to preserve JSON options.
    /// </summary>
    public virtual Dictionary<string, object> SerializeProperties()
    {
        var props = this.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<TransformationPropertyAttribute>() != null);

        var dict = new Dictionary<string, object>();
        foreach (var prop in props)
        {
            var value = prop.GetValue(this);
            if (value != null)
            {
                // For complex objects (classes), serialize to JSON element to preserve options
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    // Serialize to JSON string then parse to JsonElement
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(
                        value,
                        value.GetType(),
                        Extensions.JsonOptionsExtensions.GetDefaultOptions());

                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                    dict[prop.Name] = jsonDoc.RootElement.Clone();
                }
                else
                {
                    // Simple types can be added directly
                    dict[prop.Name] = value;
                }
            }
        }
        return dict;
    }

    /// <summary>
    /// Deserializes properties from a dictionary and sets them on this instance.
    /// </summary>
    public virtual void DeserializeProperties(Dictionary<string, object> data)
    {
        if (data == null) return;

        foreach (var kvp in data)
        {
            var prop = this.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    object? value = kvp.Value;

                    // Handle JsonElement (result of SerializeProperties for complex objects)
                    if (value is System.Text.Json.JsonElement jsonElement)
                    {
                        // Deserialize JsonElement back to the target type
                        value = System.Text.Json.JsonSerializer.Deserialize(
                            jsonElement.GetRawText(),
                            prop.PropertyType,
                            Extensions.JsonOptionsExtensions.GetDefaultOptions());
                    }
                    // Handle type conversion for simple types
                    else if (value != null && value.GetType() != prop.PropertyType)
                    {
                        if (prop.PropertyType.IsEnum)
                        {
                            value = Enum.Parse(prop.PropertyType, value.ToString()!);
                        }
                        else
                        {
                            value = Convert.ChangeType(value, prop.PropertyType);
                        }
                    }

                    prop.SetValue(this, value);
                }
                catch
                {
                    // Skip properties that can't be set
                    // Could log this for debugging
                }
            }
        }
    }

    /// <summary>
    /// Creates a deep copy of this transformation with a new ID.
    /// Useful for "Duplicate" functionality.
    /// </summary>
    public virtual TransformationBase Clone()
    {
        var clone = (TransformationBase)Activator.CreateInstance(this.GetType())!;
        clone.Id = Guid.NewGuid();
        
        var properties = SerializeProperties();
        clone.DeserializeProperties(properties);
        
        return clone;
    }
}
