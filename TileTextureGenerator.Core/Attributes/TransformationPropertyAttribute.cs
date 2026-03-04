namespace TileTextureGenerator.Core.Attributes;

/// <summary>
/// Marks a property to be automatically serialized/deserialized in transformation configuration.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class TransformationPropertyAttribute : Attribute
{
    /// <summary>
    /// Optional display name for UI purposes.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Optional description for tooltips/help.
    /// </summary>
    public string? Description { get; set; }

    public TransformationPropertyAttribute()
    {
    }

    public TransformationPropertyAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}
