using TileTextureGenerator.Adapters.Persistence.Ports;

namespace TileTextureGenerator.Adapters.Persistence.Utilities;

/// <summary>
/// Helper class for persisting images to file system and managing their JSON references.
/// Provides reusable methods for saving and loading images across different stores.
/// </summary>
public sealed class ImagePersistenceHelper
{
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// Initializes a new instance of ImagePersistenceHelper.
    /// </summary>
    /// <param name="fileStorage">File storage abstraction for I/O operations.</param>
    public ImagePersistenceHelper(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    /// <summary>
    /// Saves an image to disk and returns the JSON-compatible relative path.
    /// Generates a GUID-based filename if no explicit filename is provided.
    /// </summary>
    /// <param name="imageData">The image bytes to save.</param>
    /// <param name="baseDirectory">Base directory for the project (e.g., "Projects/MyProject").</param>
    /// <param name="subdirectory">Subdirectory within project (e.g., "Sources", "Workspace", "Outputs").</param>
    /// <param name="fileName">Optional explicit file name. If null, generates a GUID-based name.</param>
    /// <param name="existingPath">Existing JSON path to reuse (avoids generating new GUID on updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON-compatible relative path (with forward slashes).</returns>
    public async Task<string> SaveImageAsync(
        byte[] imageData,
        string baseDirectory,
        string subdirectory,
        string? fileName = null,
        string? existingPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(subdirectory);

        if (imageData.Length == 0)
            throw new ArgumentException("Image data cannot be empty.", nameof(imageData));

        // Determine filename
        string actualFileName;
        if (!string.IsNullOrEmpty(existingPath))
        {
            // Reuse existing filename from JSON path
            actualFileName = Path.GetFileName(PathHelper.ToPlatformPath(existingPath));
        }
        else if (!string.IsNullOrEmpty(fileName))
        {
            // Use provided filename
            actualFileName = fileName;
        }
        else
        {
            // Generate new GUID-based filename
            actualFileName = $"{Guid.NewGuid()}.png";
        }

        // Construct full path
        string fullPath = Path.Combine(baseDirectory, subdirectory, actualFileName);
        
        // Save to disk
        await _fileStorage.WriteAllBytesAsync(fullPath, imageData, cancellationToken);

        // Return JSON-compatible path
        string relativePath = Path.Combine(subdirectory, actualFileName);
        return PathHelper.ToJsonPath(relativePath);
    }

    /// <summary>
    /// Loads an image from disk using a JSON path reference.
    /// </summary>
    /// <param name="jsonPath">The JSON path (with forward slashes) to the image.</param>
    /// <param name="baseDirectory">Base directory for the project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The image bytes, or null if the file doesn't exist.</returns>
    public async Task<byte[]?> LoadImageAsync(
        string? jsonPath,
        string baseDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(jsonPath))
            return null;

        ArgumentNullException.ThrowIfNull(baseDirectory);

        // Convert JSON path to platform path
        string platformPath = PathHelper.ToPlatformPath(jsonPath);
        string fullPath = Path.Combine(baseDirectory, platformPath);

        // Check if file exists
        if (!await _fileStorage.FileExistsAsync(fullPath, cancellationToken))
            return null;

        // Load and return
        return await _fileStorage.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    /// <summary>
    /// Saves an image with a property-based filename (e.g., "DisplayImage.png", "SourceImage.png").
    /// </summary>
    /// <param name="imageData">The image bytes to save.</param>
    /// <param name="baseDirectory">Base directory for the project.</param>
    /// <param name="subdirectory">Subdirectory within project.</param>
    /// <param name="propertyName">Property name to use as filename (without extension).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON-compatible relative path.</returns>
    public async Task<string> SavePropertyImageAsync(
        byte[] imageData,
        string baseDirectory,
        string subdirectory,
        string propertyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be empty or whitespace.", nameof(propertyName));

        string fileName = $"{propertyName}.png";
        return await SaveImageAsync(imageData, baseDirectory, subdirectory, fileName, null, cancellationToken);
    }

    /// <summary>
    /// Deletes an image file using its JSON path reference.
    /// </summary>
    /// <param name="jsonPath">The JSON path to the image.</param>
    /// <param name="baseDirectory">Base directory for the project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteImageAsync(
        string? jsonPath,
        string baseDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(jsonPath))
            return;

        ArgumentNullException.ThrowIfNull(baseDirectory);

        string platformPath = PathHelper.ToPlatformPath(jsonPath);
        string fullPath = Path.Combine(baseDirectory, platformPath);

        await _fileStorage.DeleteFileAsync(fullPath, cancellationToken);
    }

    /// <summary>
    /// Checks if an image file exists using its JSON path reference.
    /// </summary>
    /// <param name="jsonPath">The JSON path to check.</param>
    /// <param name="baseDirectory">Base directory for the project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    public async Task<bool> ImageExistsAsync(
        string? jsonPath,
        string baseDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(jsonPath))
            return false;

        ArgumentNullException.ThrowIfNull(baseDirectory);

        string platformPath = PathHelper.ToPlatformPath(jsonPath);
        string fullPath = Path.Combine(baseDirectory, platformPath);

        return await _fileStorage.FileExistsAsync(fullPath, cancellationToken);
    }
}
