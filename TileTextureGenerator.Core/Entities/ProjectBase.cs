using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Abstract base class for all texture project types.
/// Contains common properties and behavior shared by all project types.
/// Does NOT contain serialization logic (handled by persistence layer).
/// </summary>
public abstract class ProjectBase
{
    /// <summary>
    /// Unique name of the project.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Type identifier for polymorphic instantiation (typically the class name).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the project.
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Unexisting;

    /// <summary>
    /// Display image for UI (PNG, 256x256). Nullable.
    /// </summary>
    public byte[]? DisplayImage { get; set; }

    /// <summary>
    /// Last modification timestamp (UTC).
    /// </summary>
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    protected ProjectBase(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(name));

        Name = name;
    }

    /// <summary>
    /// Sets the display image from raw image data.
    /// Converts to PNG 256x256 for display purposes using the provided image processor.
    /// </summary>
    /// <param name="imageData">Raw image data to process.</param>
    /// <param name="imageProcessor">Service to process the image.</param>
    public void SetDisplayImage(byte[] imageData, IImageProcessingService imageProcessor)
    {
        ArgumentNullException.ThrowIfNull(imageProcessor);

        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty.", nameof(imageData));

        DisplayImage = imageProcessor.ConvertToPng(imageData, 256, 256);
    }
}
