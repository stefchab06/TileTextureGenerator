using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Frontend.UI.ViewModels;
using TileTextureGenerator.Frontend.UI.Views;

namespace TileTextureGenerator.Frontend.UI.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToTransformationsManagementAsync(HorizontalTileTextureProjectEntity project)
    {
        var viewModel = new TransformationsManagementViewModel(project);
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
