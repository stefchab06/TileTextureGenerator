namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Entity representing a transformation configuration in a project.
/// Serialized to JSON as part of the project file.
/// </summary>
public class TransformationEntity
{
    /// <summary>
    /// Unique identifier for this transformation instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Fully qualified type name of the transformation class.
    /// Used to instantiate the correct TransformationBase subclass.
    /// Example: "InclinedPlaneTransformation"
    /// </summary>
    public string TransformationType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized properties of the transformation.
    /// Keys are property names, values are property values.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Paths to workspace images (relative to project Workspace/ directory).
    /// Key = logical name (e.g., "fill_texture"), Value = filename (e.g., "a1b2c3d4.png")
    /// These are intermediate images used during transformation execution.
    /// </summary>
    public Dictionary<string, string> WorkspaceImages { get; set; } = new();

    /// <summary>
    /// Path to the final output image (relative to project Output/ directory).
    /// Example: "inclined_north_0_50.png"
    /// Null if not yet generated.
    /// </summary>
    public string? OutputImagePath { get; set; }

    /// <summary>
    /// Display order in the UI and PDF output.
    /// Lower numbers appear first.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indicates whether the output image has been generated.
    /// </summary>
    public bool IsGenerated { get; set; }

    /// <summary>
    /// Timestamp of when the output image was last generated.
    /// </summary>
    public DateTime? LastGeneratedDate { get; set; }

    /// <summary>
    /// Timestamp of when this transformation was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of when this transformation was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
