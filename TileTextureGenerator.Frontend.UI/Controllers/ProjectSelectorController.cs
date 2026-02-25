using TileTextureGenerator.Frontend.UI.Views;
using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Frontend.UI.Models;
using TileTextureGenerator.Frontend.UI.Services;

namespace TileTextureGenerator.Frontend.UI.Controllers;

internal class ProjectSelectorController
{
    private IProjectSelectionUseCase _ProjectSelectionUseCase;
    public Task LoadAsync()
    {
        // Load and display TileTextureGenerator.Frontend.UI.Views.ProjectSelectorView
        return Task.CompletedTask;
    }

    public ProjectSelectorController(IProjectSelectionUseCase projectSelectionUseCase)
    {
        _ProjectSelectionUseCase = projectSelectionUseCase;
    }
}
