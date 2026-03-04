namespace TileTextureGenerator.Infrastructure.FileSystem.Transformation;

/// <summary>
/// Service for managing transformation workspace images (intermediate images used during transformation execution).
/// Handles saving, loading, and cleanup of images stored in the Workspace/ directory.
/// </summary>
public class TransformationImageService
{
    /// <summary>
    /// Saves a workspace image for a transformation.
    /// </summary>
    /// <param name="projectFolder">Full path to the project folder</param>
    /// <param name="imageData">Image data (PNG format)</param>
    /// <returns>The filename (GUID.png) that was generated</returns>
    public async Task<string> SaveWorkspaceImageAsync(string projectFolder, byte[] imageData)
    {
        var workspaceFolder = Path.Combine(projectFolder, "Workspace");
        Directory.CreateDirectory(workspaceFolder);

        var filename = $"{Guid.NewGuid()}.png";
        var filePath = Path.Combine(workspaceFolder, filename);

        await File.WriteAllBytesAsync(filePath, imageData);

        return filename;
    }

    /// <summary>
    /// Loads a workspace image.
    /// </summary>
    /// <param name="projectFolder">Full path to the project folder</param>
    /// <param name="filename">Filename (e.g., "a1b2c3d4.png")</param>
    /// <returns>Image data, or null if not found</returns>
    public async Task<byte[]?> LoadWorkspaceImageAsync(string projectFolder, string filename)
    {
        var filePath = Path.Combine(projectFolder, "Workspace", filename);

        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllBytesAsync(filePath);
    }

    /// <summary>
    /// Saves an output image for a transformation.
    /// </summary>
    /// <param name="projectFolder">Full path to the project folder</param>
    /// <param name="safeFileName">Safe filename (without extension, e.g., "flat_horizontal_2x2")</param>
    /// <param name="imageData">Image data (PNG format)</param>
    /// <returns>Relative path to the saved image (e.g., "Output/flat_horizontal_2x2.png")</returns>
    public async Task<string> SaveOutputImageAsync(string projectFolder, string safeFileName, byte[] imageData)
    {
        var outputFolder = Path.Combine(projectFolder, "Output");
        Directory.CreateDirectory(outputFolder);

        var filename = $"{safeFileName}.png";
        var filePath = Path.Combine(outputFolder, filename);

        await File.WriteAllBytesAsync(filePath, imageData);

        return Path.Combine("Output", filename);
    }

    /// <summary>
    /// Loads an output image.
    /// </summary>
    /// <param name="projectFolder">Full path to the project folder</param>
    /// <param name="relativePath">Relative path (e.g., "Output/flat_horizontal_2x2.png")</param>
    /// <returns>Image data, or null if not found</returns>
    public async Task<byte[]?> LoadOutputImageAsync(string projectFolder, string relativePath)
    {
        var filePath = Path.Combine(projectFolder, relativePath);

        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllBytesAsync(filePath);
    }

    /// <summary>
    /// Deletes a workspace image.
    /// </summary>
    /// <param name="projectFolder">Full path to the project folder</param>
    /// <param name="filename">Filename to delete</param>
    public void DeleteWorkspaceImage(string projectFolder, string filename)
    {
        var filePath = Path.Combine(projectFolder, "Workspace", filename);

        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    /// <summary>
    /// Deletes an output image.
    /// </summary>
    /// <param name="projectFolder">Full path to the project folder</param>
    /// <param name="relativePath">Relative path to the image</param>
    public void DeleteOutputImage(string projectFolder, string relativePath)
    {
        var filePath = Path.Combine(projectFolder, relativePath);

        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    /// <summary>
    /// Cleans up orphaned workspace images (images not referenced by any transformation).
    /// </summary>
    /// <param name="projectFolder">Full path to the project folder</param>
    /// <param name="referencedFilenames">Set of filenames that are still referenced</param>
    public void CleanupOrphanedWorkspaceImages(string projectFolder, HashSet<string> referencedFilenames)
    {
        var workspaceFolder = Path.Combine(projectFolder, "Workspace");

        if (!Directory.Exists(workspaceFolder))
            return;

        foreach (var file in Directory.GetFiles(workspaceFolder, "*.png"))
        {
            var filename = Path.GetFileName(file);

            // Don't delete special files like DisplayImage.png
            if (filename == "DisplayImage.png" || filename == "thumbnail.png")
                continue;

            if (!referencedFilenames.Contains(filename))
            {
                File.Delete(file);
            }
        }
    }

    /// <summary>
    /// Gets the full path to the project's workspace folder.
    /// </summary>
    public string GetWorkspaceFolder(string projectFolder)
    {
        return Path.Combine(projectFolder, "Workspace");
    }

    /// <summary>
    /// Gets the full path to the project's output folder.
    /// </summary>
    public string GetOutputFolder(string projectFolder)
    {
        return Path.Combine(projectFolder, "Output");
    }
}
