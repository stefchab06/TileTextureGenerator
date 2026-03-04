namespace TileTextureGenerator.Core.Transformations;

/// <summary>
/// Result of a transformation execution.
/// </summary>
public class TransformationResult
{
    /// <summary>
    /// Indicates whether the transformation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The final output image (PNG format).
    /// This will be saved in the Output/ directory with a descriptive name.
    /// </summary>
    public byte[]? OutputImage { get; set; }

    /// <summary>
    /// Intermediate workspace images generated during transformation.
    /// Key = logical name (for reference), Value = image bytes.
    /// These will be saved in Workspace/ with GUID filenames.
    /// </summary>
    public Dictionary<string, byte[]> WorkspaceImages { get; set; } = new();

    /// <summary>
    /// Error message if Success = false.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Non-fatal warnings that occurred during transformation.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Optional metadata about the transformation execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
