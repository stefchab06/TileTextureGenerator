using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Core.Transformations;

/// <summary>
/// Context information passed to transformation execution.
/// Contains all data needed to execute a transformation.
/// </summary>
public class ProjectContext
{
    /// <summary>
    /// Unique identifier of the project.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Full path to the project directory on filesystem.
    /// </summary>
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>
    /// The source image data (from the cropped/rotated initial image).
    /// </summary>
    public byte[] SourceImage { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The tile shape configuration (Full, HalfHorizontal, HalfVertical).
    /// </summary>
    public TileShape TileShape { get; set; }

    /// <summary>
    /// Transformation properties from the cropping editor.
    /// </summary>
    public double TranslationX { get; set; }
    public double TranslationY { get; set; }
    public double RotationAngle { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;

    /// <summary>
    /// Additional context data that specific transformations might need.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
