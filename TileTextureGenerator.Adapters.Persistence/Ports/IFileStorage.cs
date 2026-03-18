namespace TileTextureGenerator.Adapters.Persistence.Ports;

/// <summary>
/// Low-level file I/O abstraction for persistence operations.
/// Platform-agnostic file system access without domain knowledge.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Gets the base directory path where application data is stored.
    /// Platform-specific: ProgramData (Windows), /usr/share or ~/.local/share (Linux/macOS).
    //  to implement as Path.Combine(appDataPath, "TileTextureGenerator");
    /// </summary>
    /// <returns>The absolute path to the application data directory.</returns>
    string GetApplicationDataPath();

    /// <summary>
    /// Gets all projects root path.
    //  to implement as Path.Combine(GetApplicationDataPath, "Projects");
    /// </summary>
    /// <returns>The absolute path to the a project.</returns>
    string GetProjectsRootPath();

    /// <summary>
    /// Gets a project path.
    //  to implement as Path.Combine(GetProjectsRootPath, cleanProjectName);
    /// </summary>
    /// <returns>The absolute path to the a project.</returns>
    string GetProjectPath(string cleanProjectName);

    /// <summary>
    /// Gets a project json file path.
    //  to implement as Path.Combine(GetProjectPath(cleanProjectName), $"{cleanedName}.json");
    /// </summary>
    /// <returns>The absolute path to the a project.</returns>
    string GetProjectFileName(string cleanProjectName);

    /// <summary>
    /// Reads all text from a file asynchronously.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as a string.</returns>
    Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all text to a file asynchronously. Creates directories if needed.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all bytes from a file asynchronously.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as bytes.</returns>
    Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all bytes to a file asynchronously. Creates directories if needed.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="content">The bytes to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAllBytesAsync(string filePath, byte[] content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    /// <param name="directoryPath">The absolute or relative path to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a directory and all its contents if it exists.
    /// </summary>
    /// <param name="directoryPath">The absolute or relative path to the directory.</param>
    /// <param name="recursive">Whether to delete subdirectories and files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates directories in a directory.
    /// </summary>
    /// <param name="directoryPath">The absolute or relative path to the directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of directory paths.</returns>
    Task<IEnumerable<string>> GetDirectoriesAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a directory exists, creating it and parent directories if needed.
    /// </summary>
    /// <param name="directoryPath">The absolute or relative path to ensure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureDirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default);
}
