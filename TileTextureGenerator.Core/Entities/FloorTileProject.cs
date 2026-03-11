using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Concrete project entity for floor tile texture generation.
/// Handles horizontal tile layouts with configurable shapes and transformations.
/// </summary>
public sealed class FloorTileProject : ProjectBase
{
    static FloorTileProject()
    {
        TextureProjectRegistry.Register(
            key: nameof(FloorTileProject),
            factory: name => new FloorTileProject(name)
        );
    }

    /// <summary>
    /// Source image data (PNG format, full resolution).
    /// </summary>
    public byte[]? SourceImage { get; set; }

    /// <summary>
    /// Shape of the tile (Full, HalfHorizontal, HalfVertical).
    /// </summary>
    public TileShape TileShape { get; set; } = TileShape.Full;

    /// <summary>
    /// List of transformations configured for this project.
    /// Each transformation generates one output image.
    /// </summary>
    public List<TransformationEntity> Transformations { get; set; } = [];

    public FloorTileProject(string name) : base(name)
    {
        Type = nameof(FloorTileProject);
    }
}
