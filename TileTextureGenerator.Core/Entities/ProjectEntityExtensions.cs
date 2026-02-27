using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Extension methods for project entities to set display image
/// </summary>
public static class ProjectEntityExtensions
{
    /// <summary>
    /// Sets the display image for a project (accessible from workflows)
    /// </summary>
    public static void SetDisplayImage(this TileTextureProjectBase project, byte[] imageData, IImageProcessingService imageProcessor)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        
        // Use reflection to call the protected method
        var method = typeof(TileTextureProjectBase).GetMethod(
            "SetDisplayImageFromImageData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method?.Invoke(project, new object[] { imageData, imageProcessor });
    }
}
