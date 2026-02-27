using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Adapters.UseCases.Ports.Input;

/// <summary>
/// Common interface for all project-specific workflows
/// </summary>
public interface IProjectWorkflow
{
    /// <summary>
    /// Initializes a newly created project (New → Pending)
    /// </summary>
    Task InitializeAsync(TileTextureProjectBase project);
    
    /// <summary>
    /// Continues work on an existing project (stays Pending or moves to Generated)
    /// </summary>
    Task ContinueWorkAsync(TileTextureProjectBase project);
    
    /// <summary>
    /// Generates the final PDF (Pending → Generated)
    /// </summary>
    Task GeneratePdfAsync(TileTextureProjectBase project);
    
    /// <summary>
    /// Archives the project (Generated → Archived)
    /// </summary>
    Task ArchiveAsync(TileTextureProjectBase project);
}
