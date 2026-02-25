using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Adapters.UseCases.Workflows;

/// <summary>
/// Workflow implementation for horizontal tile texture generation
/// </summary>
internal class HorizontalTileTextureWorkflow : IHorizontalTileTextureWorkflow
{
    public async Task StartGenerationAsync(HorizontalTileTextureProjectEntity project)
    {
        // TODO: Implement the start workflow
        // For now, just a placeholder
        await Task.CompletedTask;
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Starting horizontal tile generation for project: {project.Name}");
#endif
    }

    public async Task ContinueGenerationAsync(HorizontalTileTextureProjectEntity project)
    {
        // TODO: Implement the continue workflow
        // For now, just a placeholder
        await Task.CompletedTask;
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Continuing horizontal tile generation for project: {project.Name}");
#endif
    }
}
