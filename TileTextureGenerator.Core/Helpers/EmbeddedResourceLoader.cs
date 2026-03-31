using System.Reflection;
using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Core.Helpers;

/// <summary>
/// Helper class to load embedded resources from the Core assembly.
/// Used primarily for loading transformation icons stored as embedded PNG files.
/// </summary>
public static class EmbeddedResourceLoader
{
    private static readonly Assembly _coreAssembly = typeof(Entities.TransformationBase).Assembly;
    private const string IconsNamespace = "TileTextureGenerator.Core.Resources.Icons";

    /// <summary>
    /// Loads an icon from embedded resources by resource name.
    /// Resource names should be just the file name (e.g., "HorizontalFloor.png").
    /// The full resource path will be constructed as: TileTextureGenerator.Core.Resources.Icons.{resourceName}
    /// </summary>
    /// <param name="resourceName">Name of the resource file (e.g., "HorizontalFloor.png")</param>
    /// <returns>ImageData containing the loaded PNG bytes</returns>
    /// <exception cref="ArgumentNullException">Thrown if resourceName is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if the resource is not found</exception>
    public static ImageData LoadIcon(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        if (string.IsNullOrWhiteSpace(resourceName))
            throw new ArgumentException("Resource name cannot be empty or whitespace.", nameof(resourceName));

        var fullResourceName = $"{IconsNamespace}.{resourceName}";

        using var stream = _coreAssembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{fullResourceName}' not found. " +
                $"Make sure the file exists in 'Core/Resources/Icons/' and is marked as 'EmbeddedResource' in the .csproj file.");
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return new ImageData(memoryStream.ToArray());
    }
}
