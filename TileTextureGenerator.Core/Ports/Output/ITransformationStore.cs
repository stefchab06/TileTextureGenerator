using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Output port for individual transformation persistence operations.
/// Generic interface allowing type-safe persistence of specific transformation types.
/// Implemented by persistence adapters (e.g., file system, database).
/// </summary>
/// <typeparam name="TTransformation">The specific transformation type to persist.</typeparam>
public interface ITransformationStore
{
    /// <summary>
    /// Persists a transformation instance to storage.
    /// </summary>
    /// <param name="transformation">The transformation to save.</param>
    Task SaveAsync(TransformationBase transformation);
}
