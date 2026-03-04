using TileTextureGenerator.Core.Attributes;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Transformations;

namespace TileTextureGenerator.Core.Transformations.Implementations;

/// <summary>
/// Flat horizontal transformation that creates a completely flat tile texture.
/// Adds rectangular flaps on all four sides (0.25" height) to be folded.
/// The visible top surface remains flat without inclination.
/// </summary>
[TransformationCardinality(MaxPerProject = 1)]
public class FlatHorizontalTransformation : TransformationBase
{
    // Auto-registration: triggers when the type is first loaded
    static FlatHorizontalTransformation()
    {
        Registries.TransformationTypeRegistry.Register<FlatHorizontalTransformation>();
    }

    /// <summary>
    /// Relative path to the base texture image in Workspace/ directory.
    /// This should reference the cropped/rotated source image from the project.
    /// Format: "guid.png"
    /// </summary>
    [TransformationProperty]
    public string? BaseTextureWorkspacePath { get; set; }

    /// <summary>
    /// Shape of the tile defining its dimensions.
    /// Determines MeridionalExtent and ZonalExtent values.
    /// </summary>
    [TransformationProperty]
    public TileShape TileShape { get; set; } = TileShape.Full;

    /// <summary>
    /// Meridional extent (North-South dimension) in inches.
    /// Calculated from TileShape:
    /// - Full: 2.0"
    /// - HalfHorizontal: 1.0"
    /// - HalfVertical: 2.0"
    /// </summary>
    public double MeridionalExtent => TileShape switch
    {
        TileShape.Full => 2.0,
        TileShape.HalfHorizontal => 1.0,
        TileShape.HalfVertical => 2.0,
        _ => 2.0
    };

    /// <summary>
    /// Zonal extent (East-West dimension) in inches.
    /// Calculated from TileShape:
    /// - Full: 2.0"
    /// - HalfHorizontal: 2.0"
    /// - HalfVertical: 1.0"
    /// </summary>
    public double ZonalExtent => TileShape switch
    {
        TileShape.Full => 2.0,
        TileShape.HalfHorizontal => 2.0,
        TileShape.HalfVertical => 1.0,
        _ => 2.0
    };

    /// <summary>
    /// Height of the flaps (edges to be folded) in inches.
    /// Standard value is 0.25" (quarter inch) for horizontal tiles.
    /// </summary>
    public const double FlapHeightInInches = 0.25;

    public override string GetDisplayName()
    {
        // TODO: Localize this using resource strings
        var shape = TileShape switch
        {
            TileShape.Full => "2\"×2\"",
            TileShape.HalfHorizontal => "1\"×2\"",
            TileShape.HalfVertical => "2\"×1\"",
            _ => "2\"×2\""
        };
        return $"Flat Horizontal ({shape})";
    }

    public override string GetSafeFileName()
    {
        var shape = TileShape switch
        {
            TileShape.Full => "2x2",
            TileShape.HalfHorizontal => "1x2",
            TileShape.HalfVertical => "2x1",
            _ => "2x2"
        };
        return $"flat_horizontal_{shape}";
    }

    public override string GetIconResourcePath()
    {
        return "Resources/Images/Transformations/flat_horizontal.png";
    }

    public override async Task<TransformationResult> ExecuteAsync(
        ProjectContext context,
        CancellationToken ct = default)
    {
        var result = new TransformationResult();

        try
        {
            // TODO: Implement actual image transformation
            // Steps:
            // 1. Load BaseTexture from workspace (or use context.SourceImage if null)
            // 2. Create canvas with dimensions:
            //    - Width: ZonalExtent + 2 * FlapHeight (flaps on East & West)
            //    - Height: MeridionalExtent + 2 * FlapHeight (flaps on North & South)
            // 3. Draw base texture in center
            // 4. For each edge (North, South, East, West):
            //    - Check EdgeFlaps[direction].Mode
            //    - Draw flap accordingly (blank, color, symmetric, or texture)
            // 5. Encode final image as PNG

            await Task.CompletedTask; // Placeholder for async work

            result.Success = false;
            result.ErrorMessage = "FlatHorizontalTransformation.ExecuteAsync() not yet implemented.";
            result.Warnings.Add("This is a placeholder implementation.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error executing flat horizontal transformation: {ex.Message}";
        }

        return result;
    }

    // TODO: Implement helper methods for image manipulation
    // private async Task<SKBitmap> LoadBaseTextureAsync(ProjectContext context) { }
    // private SKBitmap CreateCanvas() { }
    // private void DrawFlap(SKCanvas canvas, CardinalDirection direction, SKBitmap baseTexture) { }
    // private SKColor ParseHexColor(string hex) { }
    // private SKBitmap CreateSymmetricFlap(SKBitmap baseTexture, CardinalDirection direction) { }
}
