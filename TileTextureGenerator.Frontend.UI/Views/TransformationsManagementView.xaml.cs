using TileTextureGenerator.Frontend.UI.ViewModels;

namespace TileTextureGenerator.Frontend.UI.Views;

public partial class TransformationsManagementView : ContentPage
{
    private readonly TransformationsManagementViewModel _viewModel;

    public TransformationsManagementView(TransformationsManagementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Refresh the list when returning from config view
        _viewModel.Refresh();
    }
}
