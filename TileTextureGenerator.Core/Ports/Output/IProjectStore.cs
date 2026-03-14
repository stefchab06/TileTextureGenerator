namespace TileTextureGenerator.Core.Ports.Output;

/// <summary>
/// Output port for individual project persistence operations.
/// Generic interface allowing type-safe persistence of specific project types.
/// Implemented by persistence adapters (e.g., file system, database).
/// </summary>
/// <typeparam name="TProject">The specific project type to persist.</typeparam>
public interface IProjectStore<TProject> where TProject : class
{
    /// <summary>
    /// Persists a project instance to storage.
    /// </summary>
    /// <param name="project">The project to save.</param>
    Task SaveAsync(TProject project);

    /// <summary>
    /// Loads a project instance from storage.
    /// </summary>
    /// <param name="projectName">Name of the project to load.</param>
    /// <returns>The loaded project, or null if not found.</returns>
    Task<TProject?> LoadAsync(string projectName);
}
