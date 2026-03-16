namespace TileTextureGenerator.Adapters.Persistence.Utilities;

/// <summary>
/// Utility methods for file name sanitization.
/// </summary>
public static class FileNameHelper
{
    /// <summary>
    /// Cleans a file name by replacing invalid characters with underscores.
    /// Uses platform-specific invalid characters from Path.GetInvalidFileNameChars().
    /// </summary>
    /// <param name="fileName">The file name to clean.</param>
    /// <returns>A cleaned file name safe for the current platform.</returns>
    /// <exception cref="ArgumentNullException">Thrown when fileName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when fileName is empty or whitespace.</exception>
    public static string CleanFileName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty or whitespace.", nameof(fileName));

        char[] invalidChars = Path.GetInvalidFileNameChars();
        return invalidChars.Aggregate(fileName, (current, c) => current.Replace(c, '_'));
    }
}
