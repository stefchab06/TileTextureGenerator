using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Transformations.Implementations;
using TileTextureGenerator.Frontend.UI.ViewModels;

namespace TileTextureGenerator.Frontend.UI.Views;

public partial class FlatHorizontalTransformationConfigView : ContentPage
{
    public FlatHorizontalTransformationConfigView(FlatHorizontalTransformationConfigViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Initialize edge flap controls
        NorthConfig.SetConfiguration(CardinalDirection.North, viewModel.Transformation.EdgeFlaps.North);
        SouthConfig.SetConfiguration(CardinalDirection.South, viewModel.Transformation.EdgeFlaps.South);
        EastConfig.SetConfiguration(CardinalDirection.East, viewModel.Transformation.EdgeFlaps.East);
        WestConfig.SetConfiguration(CardinalDirection.West, viewModel.Transformation.EdgeFlaps.West);
    }
}
