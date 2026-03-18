using TileTextureGenerator.Adapters.Persistence.Ports;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Mocks;

/// <summary>
/// In-memory implementation of IFileStorage for testing purposes.
/// Simulates file system operations without actual I/O.
/// </summary>
public sealed class InMemoryFileStorage : IFileStorage
{
    private readonly Dictionary<string, string> _textFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> _binaryFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _appDataPath;

    public InMemoryFileStorage(string? appDataPath = null)
    {
        _appDataPath = appDataPath ?? Path.Combine("C:", "TestAppData");
        _directories.Add(_appDataPath);
    }

    public string GetApplicationDataPath() => Path.Combine(_appDataPath, "TileTextureGenerator");
    public string GetProjectsRootPath() => Path.Combine(GetApplicationDataPath(), "Projects");
    public string GetProjectPath(string projectName) => Path.Combine(GetProjectsRootPath(), projectName); 
    public string GetProjectFileName(string projectName) => Path.Combine(GetProjectPath(projectName), $"{projectName}.json");


    public Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(filePath);
        
        if (!_textFiles.TryGetValue(normalizedPath, out var content))
            throw new FileNotFoundException($"File not found: {filePath}");

        return Task.FromResult(content);
    }

    public Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(filePath);
        EnsureDirectoryExists(Path.GetDirectoryName(normalizedPath)!);
        _textFiles[normalizedPath] = content;
        return Task.CompletedTask;
    }

    public Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(filePath);
        
        if (!_binaryFiles.TryGetValue(normalizedPath, out var content))
            throw new FileNotFoundException($"File not found: {filePath}");

        return Task.FromResult(content);
    }

    public Task WriteAllBytesAsync(string filePath, byte[] content, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(filePath);
        EnsureDirectoryExists(Path.GetDirectoryName(normalizedPath)!);
        _binaryFiles[normalizedPath] = content;
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(filePath);
        bool exists = _textFiles.ContainsKey(normalizedPath) || _binaryFiles.ContainsKey(normalizedPath);
        return Task.FromResult(exists);
    }

    public Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(directoryPath);
        return Task.FromResult(_directories.Contains(normalizedPath));
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(filePath);
        _textFiles.Remove(normalizedPath);
        _binaryFiles.Remove(normalizedPath);
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(directoryPath);
        
        if (recursive)
        {
            // Remove directory and all subdirectories
            _directories.RemoveWhere(d => d.StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase));
            
            // Remove all files in directory
            var filesToRemove = _textFiles.Keys.Where(f => f.StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var file in filesToRemove)
                _textFiles.Remove(file);

            var binaryFilesToRemove = _binaryFiles.Keys.Where(f => f.StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var file in binaryFilesToRemove)
                _binaryFiles.Remove(file);
        }
        else
        {
            _directories.Remove(normalizedPath);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetDirectoriesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(directoryPath);
        
        var subdirs = _directories
            .Where(d => 
            {
                if (d.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
                    return false;
                    
                if (!d.StartsWith(normalizedPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Only direct children
                string relative = d.Substring(normalizedPath.Length + 1);
                return !relative.Contains(Path.DirectorySeparatorChar);
            })
            .ToList();

        return Task.FromResult<IEnumerable<string>>(subdirs);
    }

    public Task EnsureDirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(directoryPath);
        return Task.CompletedTask;
    }

    // Helper methods

    private void EnsureDirectoryExists(string directoryPath)
    {
        string normalizedPath = NormalizePath(directoryPath);
        
        // Add this directory and all parent directories
        string current = normalizedPath;
        while (!string.IsNullOrEmpty(current))
        {
            _directories.Add(current);
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
                break;
            current = parent;
        }
    }

    private string NormalizePath(string path)
    {
        // Normalize path separators for consistent dictionary lookups
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    // Test helpers

    public void Clear()
    {
        _textFiles.Clear();
        _binaryFiles.Clear();
        _directories.Clear();
        _directories.Add(_appDataPath);
    }

    public int GetFileCount() => _textFiles.Count + _binaryFiles.Count;
    
    public int GetDirectoryCount() => _directories.Count;
}
