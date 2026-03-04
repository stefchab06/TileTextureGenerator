namespace TileTextureGenerator.Core.Attributes;

/// <summary>
/// Defines the maximum number of instances of a transformation type allowed per project.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TransformationCardinalityAttribute : Attribute
{
    /// <summary>
    /// Maximum number of instances allowed per project.
    /// Default is int.MaxValue (unlimited).
    /// </summary>
    public int MaxPerProject { get; set; } = int.MaxValue;

    public TransformationCardinalityAttribute()
    {
    }

    public TransformationCardinalityAttribute(int maxPerProject)
    {
        MaxPerProject = maxPerProject;
    }
}
