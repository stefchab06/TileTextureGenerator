using TileTextureGenerator.Presentation.UI.Services;
using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Templates;

/// <summary>
/// Template for displaying FloorTileProject properties.
/// </summary>
public partial class FloorTileTemplate : ContentView
{
    public FloorTileTemplate()
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
            BindingContext is FloorTileProjectViewModel viewModel)
        {
            viewModel.SelectTileShapeCommand.Execute(item);
        }
    }
}
