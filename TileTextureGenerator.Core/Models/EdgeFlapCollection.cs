using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Core.Models;

/// <summary>
/// Collection of edge flap configurations for a transformation.
/// Provides indexed access to the four edge flaps (Top, Right, Bottom, Left).
/// </summary>
public sealed class EdgeFlapCollection
{
    private readonly EdgeFlapConfiguration[] _edgeFlaps;

    /// <summary>
    /// Initializes a new instance of EdgeFlapCollection with default configurations.
    /// </summary>
    public EdgeFlapCollection()
    {
        _edgeFlaps = new EdgeFlapConfiguration[4];
        
        // Initialize all edge flaps with defaults
        for (int i = 0; i < 4; i++)
        {
            _edgeFlaps[i] = new EdgeFlapConfiguration();
        }
    }

    /// <summary>
    /// Indexer for accessing edge flap configurations by image side.
    /// </summary>
    /// <param name="side">The side of the image (Top, Right, Bottom, Left).</param>
    /// <returns>The edge flap configuration for the specified side.</returns>
    public EdgeFlapConfiguration this[ImageSide side]
    {
        get => _edgeFlaps[(int)side];
        set => _edgeFlaps[(int)side] = value ?? throw new ArgumentNullException(nameof(value));
    }
}
