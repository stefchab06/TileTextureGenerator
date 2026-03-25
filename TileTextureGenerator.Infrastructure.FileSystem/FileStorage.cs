using TileTextureGenerator.Adapters.Persistence.Ports;

namespace TileTextureGenerator.Infrastructure.FileSystem;

/// <summary>
/// Platform-aware file storage implementation.
/// Handles low-level file I/O operations with automatic path separator conversion.
/// </summary>
public class FileStorage : IFileStorage
{
    private readonly string _applicationDataPath;

    public FileStorage()
    {
        _applicationDataPath = InitializeApplicationDataPath();
    }

    /// <summary>
    /// Initializes the application data path based on the current platform.
    /// Windows: %ProgramData%\TileTextureGenerator
    /// Linux/macOS: ~/.local/share/TileTextureGenerator
    /// </summary>
    private static string InitializeApplicationDataPath()
    {
        if (OperatingSystem.IsWindows())
        {
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "TileTextureGenerator");
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".local", "share", "TileTextureGenerator");
        }
        else
        {
            throw new PlatformNotSupportedException("Current platform is not supported.");
        }
    }

    /// <inheritdoc />
    public string GetApplicationDataPath() => _applicationDataPath;

    /// <inheritdoc />
    public string GetProjectsRootPath() => Path.Combine(_applicationDataPath, "Projects");

    /// <inheritdoc />
    public string GetProjectPath(string cleanProjectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cleanProjectName);
        return Path.Combine(GetProjectsRootPath(), cleanProjectName);
    }

    /// <inheritdoc />
    public string GetProjectFileName(string cleanProjectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cleanProjectName);
        return Path.Combine(GetProjectPath(cleanProjectName), $"{cleanProjectName}.json");
    }

    /// <inheritdoc />
    public async Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string normalizedPath = NormalizePath(filePath);
        return await File.ReadAllTextAsync(normalizedPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(content);

        string normalizedPath = NormalizePath(filePath);
        string? directory = Path.GetDirectoryName(normalizedPath);
        
        if (!string.IsNullOrEmpty(directory))
        {
            await EnsureDirectoryExistsAsync(directory, cancellationToken);
        }

        await File.WriteAllTextAsync(normalizedPath, content, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string normalizedPath = NormalizePath(filePath);
        return await File.ReadAllBytesAsync(normalizedPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteAllBytesAsync(string filePath, byte[] content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(content);

        string normalizedPath = NormalizePath(filePath);
        string? directory = Path.GetDirectoryName(normalizedPath);
        
        if (!string.IsNullOrEmpty(directory))
        {
            await EnsureDirectoryExistsAsync(directory, cancellationToken);
        }

        await File.WriteAllBytesAsync(normalizedPath, content, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string normalizedPath = NormalizePath(filePath);
        return Task.FromResult(File.Exists(normalizedPath));
    }

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        string normalizedPath = NormalizePath(directoryPath);
        return Task.FromResult(Directory.Exists(normalizedPath));
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string normalizedPath = NormalizePath(filePath);
        
        if (File.Exists(normalizedPath))
        {
            File.Delete(normalizedPath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        string normalizedPath = NormalizePath(directoryPath);
        
        if (Directory.Exists(normalizedPath))
        {
            Directory.Delete(normalizedPath, recursive);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetDirectoriesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        string normalizedPath = NormalizePath(directoryPath);
        
        if (!Directory.Exists(normalizedPath))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        IEnumerable<string> directories = Directory.GetDirectories(normalizedPath);
        return Task.FromResult(directories);
    }

    /// <inheritdoc />
    public Task EnsureDirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        string normalizedPath = NormalizePath(directoryPath);
        
        if (!Directory.Exists(normalizedPath))
        {
            Directory.CreateDirectory(normalizedPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Normalizes path separators to platform-specific format.
    /// Converts forward slashes (/) to platform directory separator.
    /// </summary>
    /// <param name="path">Path with forward slashes (Adapters.Persistence format)</param>
    /// <returns>Path with platform-specific separators</returns>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Replace forward slashes with platform directory separator
        return path.Replace('/', Path.DirectorySeparatorChar);
    }
}
