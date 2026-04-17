using TileTextureGenerator.Presentation.UI.Services;
using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Templates;

/// <summary>
/// Template for displaying WallTileProject properties.
/// </summary>
public partial class WallTileTemplate : ContentView
{
    public WallTileTemplate()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles TileShape selection from CollectionView.
    /// </summary>
    private void OnTileShapeItemTapped(object sender, EventArgs e)
    {
        if (sender is Label label && 
            label.BindingContext is TileShapeItem item &&
            BindingContext is WallTileProjectViewModel viewModel)
        {
            viewModel.SelectTileShapeCommand.Execute(item);
        }
    }
}
