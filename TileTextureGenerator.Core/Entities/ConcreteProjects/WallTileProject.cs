using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Entities.ConcreteProjects;

/// <summary>
/// Concrete project entity for wall tile texture generation.
/// Handles vertical tile layouts with configurable shapes and transformations.
/// </summary>
public sealed class WallTileProject : ProjectBase
{
    private readonly IProjectStore<WallTileProject> _wallStore;

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
    public byte[]? SourceImage { get; set; }

    /// <summary>
    /// Shape of the tile (Full, HalfHorizontal, HalfVertical).
    /// </summary>
    public TileShape TileShape { get; set; } = TileShape.Full;

    /// <summary>
    /// Constructor with dependency injection.
    /// Store is injected by DI container.
    /// </summary>
    /// <param name="store">The WallTileProject-specific store.</param>
    public WallTileProject(IProjectStore<WallTileProject> store) 
        : base(new WallTileProjectStoreAdapter(store))
    {
        _wallStore = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <inheritdoc />
    public override async Task SaveChangesAsync()
    {
        LastModifiedDate = DateTime.UtcNow;
        await _wallStore.SaveAsync(this);
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

    /// <summary>
    /// Adapter to allow WallTileProject-specific store to be used as ProjectBase store.
    /// </summary>
    private class WallTileProjectStoreAdapter : IProjectStore<ProjectBase>
    {
        private readonly IProjectStore<WallTileProject> _innerStore;

        public WallTileProjectStoreAdapter(IProjectStore<WallTileProject> innerStore)
        {
            _innerStore = innerStore;
        }

        public async Task SaveAsync(ProjectBase project)
        {
            if (project is not WallTileProject wallProject)
                throw new InvalidOperationException($"Expected WallTileProject but got {project.GetType().Name}");

            await _innerStore.SaveAsync(wallProject);
        }

        public async Task<ProjectBase?> LoadAsync(string projectName)
        {
            return await _innerStore.LoadAsync(projectName);
        }
    }
}
