using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Entities.ConcreteProjects;

/// <summary>
/// Concrete project entity for floor tile texture generation.
/// Handles horizontal tile layouts with configurable shapes and transformations.
/// </summary>
public sealed class FloorTileProject : ProjectBase
{
    private readonly IProjectStore<FloorTileProject> _floorStore;

    private static readonly string[] AvailableTransformationTypes = 
    {
        nameof(ConcreteTransformations.HorizontalFloorTransformation)
    };

    static FloorTileProject()
    {
        TextureProjectRegistry.RegisterType<FloorTileProject>();
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
    /// <param name="store">The FloorTileProject-specific store.</param>
    public FloorTileProject(IProjectStore<FloorTileProject> store) 
        : base(new FloorTileProjectStoreAdapter(store))
    {
        _floorStore = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <inheritdoc />
    public override async Task SaveChangesAsync()
    {
        LastModifiedDate = DateTime.UtcNow;
        await _floorStore.SaveAsync(this);
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
    /// Adapter to allow FloorTileProject-specific store to be used as ProjectBase store.
    /// </summary>
    private class FloorTileProjectStoreAdapter : IProjectStore<ProjectBase>
    {
        private readonly IProjectStore<FloorTileProject> _innerStore;

        public FloorTileProjectStoreAdapter(IProjectStore<FloorTileProject> innerStore)
        {
            _innerStore = innerStore;
        }

        public async Task SaveAsync(ProjectBase project)
        {
            if (project is not FloorTileProject floorProject)
                throw new InvalidOperationException($"Expected FloorTileProject but got {project.GetType().Name}");

            await _innerStore.SaveAsync(floorProject);
        }

        public async Task<ProjectBase?> LoadAsync(string projectName)
        {
            return await _innerStore.LoadAsync(projectName);
        }
    }
}
