using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Adapters.UseCases.Ports.Input;

/// <summary>
/// Use case for horizontal tile texture generation workflow
/// </summary>
public interface IHorizontalTileTextureWorkflow
{
    /// <summary>
    /// Starts the horizontal tile texture generation workflow
    /// </summary>
    Task StartGenerationAsync(HorizontalTileTextureProjectEntity project);
    
    /// <summary>
    /// Continues the horizontal tile texture generation workflow
    /// </summary>
    Task ContinueGenerationAsync(HorizontalTileTextureProjectEntity project);
}
