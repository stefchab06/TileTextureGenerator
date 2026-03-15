using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Abstract base class for all texture project types.
/// Contains common properties and behavior shared by all project types.
/// Implements IProjectManager for self-management capabilities.
/// Does NOT contain serialization logic (handled by persistence layer).
/// </summary>
public abstract class ProjectBase : IProjectManager
{
    private readonly IProjectStore<ProjectBase> _store;
    private string? _name;
    private bool _initialized;

    /// <summary>
    /// Unique name of the project.
    /// Immutable after initialization.
    /// </summary>
    public string Name 
    { 
        get => _name ?? throw new InvalidOperationException("Project not initialized. Call Initialize first.");
        private set
        {
            if (_initialized)
                throw new InvalidOperationException("Cannot modify name after initialization.");
            _name = value;
        }
    }

    /// <summary>
    /// Type identifier for polymorphic instantiation (typically the class name).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the project.
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Unexisting;

    /// <summary>
    /// Display image for UI (PNG, 256x256). Nullable.
    /// </summary>
    public byte[]? DisplayImage { get; set; }

    /// <summary>
    /// Last modification timestamp (UTC).
    /// </summary>
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// List of transformations configured for this project.
    /// Each transformation generates one output image.
    /// </summary>
    public List<TransformationEntity> Transformations { get; set; } = [];

    /// <summary>
    /// Constructor with dependency injection.
    /// Store is injected by DI container.
    /// </summary>
    /// <param name="store">The project store for persistence operations.</param>
    protected ProjectBase(IProjectStore<ProjectBase> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Initializes the project with runtime data (name).
    /// Must be called once after construction and before any other operations.
    /// </summary>
    /// <param name="name">Unique name for the project.</param>
    public void Initialize(string name)
    {
        if (_initialized)
            throw new InvalidOperationException("Project already initialized.");

        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(name));

        Name = name;
        Type = GetType().Name;
        _initialized = true;
    }

    /// <summary>
    /// Sets the display image from raw image data.
    /// Converts to PNG 256x256 for display purposes using the provided image processor.
    /// </summary>
    /// <param name="imageData">Raw image data to process.</param>
    /// <param name="imageProcessor">Service to process the image.</param>
    public void SetDisplayImage(byte[] imageData, IImageProcessingService imageProcessor)
    {
        ArgumentNullException.ThrowIfNull(imageProcessor);

        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty.", nameof(imageData));

        DisplayImage = imageProcessor.ConvertToPng(imageData, 256, 256);
    }

    /// <inheritdoc />
    public virtual async Task SaveChangesAsync()
    {
        if (!_initialized)
            throw new InvalidOperationException("Project not initialized. Call Initialize first.");

        LastModifiedDate = DateTime.UtcNow;
        await _store.SaveAsync(this);
    }

    /// <inheritdoc />
    public virtual async Task AddTransformationAsync(TransformationEntity transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);

        if (Transformations.Any(t => t.Id == transformation.Id))
            throw new InvalidOperationException($"Transformation with ID '{transformation.Id}' already exists.");

        transformation.DisplayOrder = Transformations.Count;
        Transformations.Add(transformation);
        await SaveChangesAsync();
    }

    /// <inheritdoc />
    public virtual async Task RemoveTransformationAsync(Guid transformationId)
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
    public virtual async Task ReorderTransformationsAsync(IReadOnlyList<Guid> newOrder)
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

    /// <inheritdoc />
    public abstract Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync();
}
