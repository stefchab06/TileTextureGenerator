using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Core.Transformations;

/// <summary>
/// Collection of edge flap configurations for all four cardinal directions.
/// Provides both property and indexer access.
/// </summary>
public class EdgeFlapsCollection
{
    /// <summary>
    /// North edge flap configuration (top of tile).
    /// </summary>
    public EdgeFlapConfiguration North { get; set; } = new();

    /// <summary>
    /// South edge flap configuration (bottom of tile).
    /// </summary>
    public EdgeFlapConfiguration South { get; set; } = new();

    /// <summary>
    /// East edge flap configuration (right side of tile).
    /// </summary>
    public EdgeFlapConfiguration East { get; set; } = new();

    /// <summary>
    /// West edge flap configuration (left side of tile).
    /// </summary>
    public EdgeFlapConfiguration West { get; set; } = new();

    /// <summary>
    /// Indexer to access edge flap configuration by cardinal direction.
    /// Allows syntax like: edgeFlaps[CardinalDirection.North].Mode
    /// </summary>
    public EdgeFlapConfiguration this[CardinalDirection direction]
    {
        get => direction switch
        {
            CardinalDirection.North => North,
            CardinalDirection.South => South,
            CardinalDirection.East => East,
            CardinalDirection.West => West,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
        set
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    North = value;
                    break;
                case CardinalDirection.South:
                    South = value;
                    break;
                case CardinalDirection.East:
                    East = value;
                    break;
                case CardinalDirection.West:
                    West = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
