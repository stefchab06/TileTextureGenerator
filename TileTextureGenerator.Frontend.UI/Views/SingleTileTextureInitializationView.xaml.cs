using TileTextureGenerator.Frontend.UI.ViewModels;

namespace TileTextureGenerator.Frontend.UI.Views;

public partial class SingleTileTextureInitializationView : ContentPage
{
    private readonly SingleTileTextureInitializationViewModel _viewModel;

    public SingleTileTextureInitializationView(SingleTileTextureInitializationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // Detect orientation and show appropriate layout
        bool isLandscape = width > height;

        LandscapeLayout.IsVisible = isLandscape;
        PortraitLayout.IsVisible = !isLandscape;
    }

    // TODO: Add keyboard shortcuts when .NET MAUI provides stable keyboard APIs
    // For now, users can use the "Paste" button which responds to clipboard changes
}
