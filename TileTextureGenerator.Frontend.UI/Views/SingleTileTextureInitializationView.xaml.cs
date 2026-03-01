using TileTextureGenerator.Frontend.UI.ViewModels;

namespace TileTextureGenerator.Frontend.UI.Views;

public partial class SingleTileTextureInitializationView : ContentPage
{
    public SingleTileTextureInitializationView(SingleTileTextureInitializationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // Detect orientation and show appropriate layout
        bool isLandscape = width > height;

        LandscapeLayout.IsVisible = isLandscape;
        PortraitLayout.IsVisible = !isLandscape;
    }
}
