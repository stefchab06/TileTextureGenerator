namespace TileTextureGenerator.Adapters.Persistence.Utilities;

/// <summary>
/// Utility methods for cross-platform path handling in serialization.
/// Ensures JSON files use forward slashes (/) for portability across Windows, Linux, and macOS.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Converts a platform-specific path to a portable JSON path (forward slashes).
    /// Use this when serializing paths to JSON for storage.
    /// </summary>
    /// <param name="platformPath">Path with platform-specific separators (e.g., backslashes on Windows).</param>
    /// <returns>Path with forward slashes for JSON storage, or empty string if input is null/empty.</returns>
    /// <example>
    /// Windows input: "Workspace\Projects\MyProject\file.png"
    /// Output: "Workspace/Projects/MyProject/file.png"
    /// </example>
    public static string ToJsonPath(string? platformPath)
    {
        if (string.IsNullOrEmpty(platformPath))
            return string.Empty;

        // Replace Windows backslashes with forward slashes
        return platformPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Converts a portable JSON path (forward slashes) to a platform-specific path.
    /// Use this when deserializing paths from JSON to use in file operations.
    /// </summary>
    /// <param name="jsonPath">Path with forward slashes from JSON storage.</param>
    /// <returns>Path with platform-specific separators (backslashes on Windows, forward slashes on Unix).</returns>
    /// <example>
    /// JSON input: "Workspace/Projects/MyProject/file.png"
    /// Windows output: "Workspace\Projects\MyProject\file.png"
    /// Linux output: "Workspace/Projects/MyProject/file.png"
    /// </example>
    public static string ToPlatformPath(string? jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath))
            return string.Empty;

        // Replace forward slashes with platform separator
        return jsonPath.Replace('/', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Converts a collection of platform paths to JSON paths.
    /// Use this when serializing multiple paths to JSON.
    /// </summary>
    /// <param name="platformPaths">Collection of paths with platform-specific separators.</param>
    /// <returns>Collection of paths with forward slashes.</returns>
    public static IEnumerable<string> ToJsonPaths(IEnumerable<string> platformPaths)
    {
        ArgumentNullException.ThrowIfNull(platformPaths);
        return platformPaths.Select(ToJsonPath);
    }

    /// <summary>
    /// Converts a collection of JSON paths to platform paths.
    /// Use this when deserializing multiple paths from JSON.
    /// </summary>
    /// <param name="jsonPaths">Collection of paths with forward slashes.</param>
    /// <returns>Collection of paths with platform-specific separators.</returns>
    public static IEnumerable<string> ToPlatformPaths(IEnumerable<string> jsonPaths)
    {
        ArgumentNullException.ThrowIfNull(jsonPaths);
        return jsonPaths.Select(ToPlatformPath);
    }
}
