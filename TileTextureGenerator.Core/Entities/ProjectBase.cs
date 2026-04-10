using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
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
    private readonly IProjectStore _store;
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
    /// Read-only publicly. Modified only via SetDisplayImage() by derived classes.
    /// </summary>
    public ImageData? DisplayImage { get; private set; }

    /// <summary>
    /// Last modification timestamp (UTC).
    /// </summary>
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// List of transformations configured for this project.
    /// Each transformation generates one output image.
    /// Order is determined by position in the list (index 0 is first, 1 is second, etc.).
    /// </summary>
    public List<TransformationDTO> Transformations { get; set; } = [];

    /// <summary>
    /// Constructor with dependency injection.
    /// Store is injected by DI container.
    /// </summary>
    /// <param name="store">The project store for persistence operations.</param>
    protected ProjectBase(IProjectStore store)
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
    /// Sets the display image from source image data.
    /// Resizes to 256x256 PNG for display purposes.
    /// Protected method - only derived classes can update DisplayImage.
    /// </summary>
    /// <param name="sourceImage">Source image data (will be resized to 256x256).</param>
    protected void SetDisplayImage(ImageData sourceImage)
    {
        DisplayImage = Services.ImageProcessor.ResizeImage(sourceImage, 256, 256);
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
    public virtual async Task AddTransformationAsync(string transformationType)
    {
        ArgumentNullException.ThrowIfNull(transformationType);

        // Get available transformation types to find the icon
        var availableTypes = await GetAvailableTransformationTypesAsync();
        var typeInfo = availableTypes.FirstOrDefault(t => t.Name == transformationType);

        var transformation = new TransformationDTO
        {
            Id = Guid.NewGuid(),
            Type = transformationType,
            Icon = typeInfo?.Icon // Use icon from type registry
        };

        while (Transformations.Any(t => t.Id == transformation.Id))
        {
            transformation = new TransformationDTO
            {
                Id = Guid.NewGuid(),
                Type = transformationType,
                Icon = typeInfo?.Icon
            };
        }

        Transformations.Add(transformation);
        await _store.AddTransformationAsync(this, transformation);
        LastModifiedDate = DateTime.UtcNow;
        await _store.SaveAsync(this);
    }

    /// <inheritdoc />
    public virtual async Task RemoveTransformationAsync(Guid transformationId)
    {
        var transformation = Transformations.FirstOrDefault(t => t.Id == transformationId);
        if (transformation == null)
            throw new InvalidOperationException($"Transformation with ID '{transformationId}' not found.");

        Transformations.Remove(transformation);
        await _store.RemoveTransformationAsync(this, transformationId);
        LastModifiedDate = DateTime.UtcNow;
        await _store.SaveAsync(this);
    }

    /// <inheritdoc />
    public virtual async Task<TransformationBase> GetTransformationAsync(Guid transformationId)
    {
        return await _store.LoadTransformationAsync(this, transformationId)
            ?? throw new InvalidOperationException($"Transformation with ID '{transformationId}' not found.");
    }


    /// <inheritdoc />
    public abstract Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync();

    /// <inheritdoc />
    public async Task<bool>? GenerateAsync()
    {
        // TODO: implement it.
        // Verify each tranformation has been executed and has an output image.
        // Generate a pdf with all the output images and save it to disk.
        Status = ProjectStatus.Generated;
        LastModifiedDate = DateTime.UtcNow;
        await _store.SaveAsync(this);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveAsync()
    {
        if (!_initialized)
            throw new InvalidOperationException("Project not initialized. Call Initialize first.");

        // Update project metadata before archiving
        Status = ProjectStatus.Archived;
        LastModifiedDate = DateTime.UtcNow;

        // Delegate to store for workspace deletion and JSON reduction
        await _store.ArchiveAsync(this);

        return true;
    }
}
