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
    /// List of transformations configured for this project.
    /// Each transformation generates one output image.
    /// </summary>
    public List<TransformationEntity> Transformations { get; set; } = [];

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
    public override async Task AddTransformationAsync(TransformationEntity transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);

        if (Transformations.Any(t => t.Id == transformation.Id))
            throw new InvalidOperationException($"Transformation with ID '{transformation.Id}' already exists.");

        transformation.DisplayOrder = Transformations.Count;
        Transformations.Add(transformation);
        await SaveChangesAsync();
    }

    /// <inheritdoc />
    public override async Task RemoveTransformationAsync(Guid transformationId)
    {
        var transformation = Transformations.FirstOrDefault(t => t.Id == transformationId);
        if (transformation == null)
            throw new InvalidOperationException($"Transformation with ID '{transformationId}' not found.");

        Transformations.Remove(transformation);

        // Reorder remaining transformations
        for (int i = 0; i < Transformations.Count; i++)
        {
            Transformations[i].DisplayOrder = i;
        }

        await SaveChangesAsync();
    }

    /// <inheritdoc />
    public override async Task ReorderTransformationsAsync(IReadOnlyList<Guid> newOrder)
    {
        ArgumentNullException.ThrowIfNull(newOrder);

        if (newOrder.Count != Transformations.Count)
            throw new ArgumentException("New order must contain all transformation IDs.", nameof(newOrder));

        var reordered = new List<TransformationEntity>();
        foreach (var id in newOrder)
        {
            var transformation = Transformations.FirstOrDefault(t => t.Id == id);
            if (transformation == null)
                throw new ArgumentException($"Transformation with ID '{id}' not found.", nameof(newOrder));

            transformation.DisplayOrder = reordered.Count;
            reordered.Add(transformation);
        }

        Transformations = reordered;
        await SaveChangesAsync();
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
