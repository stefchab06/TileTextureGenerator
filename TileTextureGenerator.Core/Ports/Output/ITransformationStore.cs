namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Output port for individual transformation persistence operations.
/// Generic interface allowing type-safe persistence of specific transformation types.
/// Implemented by persistence adapters (e.g., file system, database).
/// </summary>
/// <typeparam name="TTransformation">The specific transformation type to persist.</typeparam>
public interface ITransformationStore<TTransformation> where TTransformation : class
{
    /// <summary>
    /// Persists a transformation instance to storage.
    /// </summary>
    /// <param name="transformation">The transformation to save.</param>
    Task SaveAsync(TTransformation transformation);

    /// <summary>
    /// Loads a transformation instance from storage by its ID.
    /// </summary>
    /// <param name="transformationId">ID of the transformation to load.</param>
    /// <returns>The loaded transformation, or null if not found.</returns>
    Task<TTransformation?> LoadAsync(Guid transformationId);
}
