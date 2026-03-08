using TileTextureGenerator.Adapters.Persistence.Ports.Output;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Frontend.UI.ViewModels;
using TileTextureGenerator.Frontend.UI.Views;

namespace TileTextureGenerator.Frontend.UI.Services;

public class NavigationService : INavigationService
{
    private readonly IProjectPersister _projectPersister;

    public NavigationService(IProjectPersister projectPersister)
    {
        _projectPersister = projectPersister;
    }

    public async Task NavigateToTransformationsManagementAsync(HorizontalTileTextureProjectEntity project)
    {
        var viewModel = new TransformationsManagementViewModel(
            project,
            _projectPersister);
        var view = new TransformationsManagementView(viewModel);

        if (Application.Current?.MainPage?.Navigation != null)
        {
            await Application.Current.MainPage.Navigation.PushAsync(view);
        }
    }

    public async Task NavigateBackAsync()
    {
        if (Application.Current?.MainPage?.Navigation != null)
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}
