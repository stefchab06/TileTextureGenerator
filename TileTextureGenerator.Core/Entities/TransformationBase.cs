using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;

namespace TileTextureGenerator.Core.Entities;

/// <summary>
/// Abstract base class for all transformation types.
/// Contains common properties and behavior shared by all transformations.
/// Implements ITransformationManager for self-management capabilities.
/// Does NOT contain serialization logic (handled by persistence layer).
/// </summary>
public abstract class TransformationBase : ITransformationManager
{
    private readonly ITransformationStore<TransformationBase> _store;
    private ProjectBase? _parentProject;
    private Guid? _id;
    private bool _initialized;

    /// <summary>
    /// Parent project that owns this transformation.
    /// Immutable after initialization.
    /// </summary>
    public ProjectBase ParentProject
    {
        get => _parentProject ?? throw new InvalidOperationException("Transformation not initialized. Call Initialize first.");
        private set
        {
            if (_initialized)
                throw new InvalidOperationException("Cannot modify parent project after initialization.");
            _parentProject = value;
        }
    }

    /// <summary>
    /// Unique identifier of the transformation.
    /// Immutable after initialization.
    /// </summary>
    public Guid Id
    {
        get => _id ?? throw new InvalidOperationException("Transformation not initialized. Call Initialize first.");
        private set
        {
            if (_initialized)
                throw new InvalidOperationException("Cannot modify ID after initialization.");
            _id = value;
        }
    }

    /// <summary>
    /// Type identifier for polymorphic instantiation (typically the class name).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Icon representing this transformation type in the UI (PNG format, typically 32x32 or 64x64).
    /// Must be overridden in concrete classes.
    /// </summary>
    public abstract ImageData? Icon { get; }

    /// <summary>
    /// Type of paper required for printing this transformation output.
    /// Default is Standard paper. Override in concrete classes for transformations requiring heavy cardstock.
    /// </summary>
    public virtual PaperType RequiredPaperType => PaperType.Standard;

    /// <summary>
    /// Collection of edge flap configurations for all four sides.
    /// Access configurations using EdgeFlap[ImageSide.Top], EdgeFlap[ImageSide.Right], etc.
    /// </summary>
    public EdgeFlapCollection EdgeFlap { get; } = new EdgeFlapCollection();

    /// <summary>
    /// Constructor with dependency injection.
    /// Store is injected by DI container.
    /// </summary>
    /// <param name="store">The transformation store for persistence operations.</param>
    protected TransformationBase(ITransformationStore<TransformationBase> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Initializes the transformation with its parent project and unique identifier.
    /// Must be called once after construction and before any other operations.
    /// </summary>
    /// <param name="parentProject">The project that owns this transformation.</param>
    /// <param name="id">Unique identifier for the transformation.</param>
    public void Initialize(ProjectBase parentProject, Guid id)
    {
        ArgumentNullException.ThrowIfNull(parentProject);

        if (_initialized)
            throw new InvalidOperationException("Transformation already initialized.");

        if (id == Guid.Empty)
            throw new ArgumentException("Transformation ID cannot be empty.", nameof(id));

        ParentProject = parentProject;
        Id = id;
        Type = GetType().Name;
        _initialized = true;
    }

    /// <inheritdoc />
    public virtual async Task SaveChangesAsync()
    {
        if (!_initialized)
            throw new InvalidOperationException("Transformation not initialized. Call Initialize first.");

        await _store.SaveAsync(this);
    }

    /// <inheritdoc />
    public abstract Task<ImageData> ExecuteAsync();
}
