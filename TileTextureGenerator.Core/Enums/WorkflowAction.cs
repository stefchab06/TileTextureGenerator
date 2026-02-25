namespace TileTextureGenerator.Core.Enums;

/// <summary>
/// Workflow actions returned by project entities to indicate what use case should be executed
/// </summary>
public enum WorkflowAction
{
    /// <summary>
    /// No workflow action required
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Start horizontal tile texture generation workflow
    /// </summary>
    StartHorizontalTileGeneration,
    
    /// <summary>
    /// Continue horizontal tile texture generation workflow
    /// </summary>
    ContinueHorizontalTileGeneration
}
