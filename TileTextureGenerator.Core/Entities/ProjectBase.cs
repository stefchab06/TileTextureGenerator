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
    public abstract Task AddTransformationAsync(TransformationEntity transformation);

    /// <inheritdoc />
    public abstract Task RemoveTransformationAsync(Guid transformationId);

    /// <inheritdoc />
    public abstract Task ReorderTransformationsAsync(IReadOnlyList<Guid> newOrder);
}
