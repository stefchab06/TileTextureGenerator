using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Transformations;

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
    private readonly EdgeFlapConfiguration[] _edgeFlaps;
    private Guid? _id;
    private bool _initialized;

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
    public abstract byte[]? Icon { get; }

    /// <summary>
    /// Type of paper required for printing this transformation output.
    /// Default is Standard paper. Override in concrete classes for transformations requiring heavy cardstock.
    /// </summary>
    public virtual PaperType RequiredPaperType => PaperType.Standard;

    /// <summary>
    /// Indexer for accessing edge flap configurations by image side.
    /// </summary>
    /// <param name="side">The side of the image (Top, Right, Bottom, Left).</param>
    /// <returns>The edge flap configuration for the specified side.</returns>
    public EdgeFlapConfiguration this[ImageSide side]
    {
        get => _edgeFlaps[(int)side];
        set => _edgeFlaps[(int)side] = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// Store is injected by DI container.
    /// </summary>
    /// <param name="store">The transformation store for persistence operations.</param>
    protected TransformationBase(ITransformationStore<TransformationBase> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _edgeFlaps = new EdgeFlapConfiguration[4];

        // Initialize all edge flaps with defaults
        for (int i = 0; i < 4; i++)
        {
            _edgeFlaps[i] = new EdgeFlapConfiguration();
        }
    }

    /// <summary>
    /// Initializes the transformation with its unique identifier.
    /// Must be called once after construction and before any other operations.
    /// </summary>
    /// <param name="id">Unique identifier for the transformation.</param>
    public void Initialize(Guid id)
    {
        if (_initialized)
            throw new InvalidOperationException("Transformation already initialized.");

        if (id == Guid.Empty)
            throw new ArgumentException("Transformation ID cannot be empty.", nameof(id));

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
    public abstract Task<byte[]> ExecuteAsync();
}
