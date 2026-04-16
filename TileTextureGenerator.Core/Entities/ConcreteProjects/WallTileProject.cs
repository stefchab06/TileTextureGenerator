using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities.ConcreteTransformations;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Entities.ConcreteProjects;

/// <summary>
/// Concrete project entity for wall tile texture generation.
/// Handles vertical tile layouts with configurable shapes and transformations.
/// </summary>
public sealed class WallTileProject : ProjectBase
{
    private static readonly string[] AvailableTransformationTypes =
    {
        nameof(ConcreteTransformations.VerticalWallTransformation)
    };

    static WallTileProject()
    {
        TextureProjectRegistry.RegisterType<WallTileProject>();
    }

    /// <summary>
    /// Source image data (PNG format, full resolution).
    /// </summary>
    public ImageData? SourceImage { get; set; }

    /// <summary>
    /// Shape of the tile (Full, HalfHorizontal, HalfVertical).
    /// </summary>
    public TileShape TileShape { get; set; } = TileShape.Full;

    /// <summary>
    /// Constructor with dependency injection.
    /// Store is injected by DI container.
    /// </summary>
    /// <param name="store">The project store for persistence operations.</param>
    public WallTileProject(IProjectStore store) 
        : base(store)
    {
    }

    /// <inheritdoc />
    public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
    {
        var dtos = AvailableTransformationTypes
            .Select(name => new TransformationTypeDTO
            {
                Name = name,
                Icon = TransformationTypeRegistry.GetIcon(name)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>(dtos);
    }
}
