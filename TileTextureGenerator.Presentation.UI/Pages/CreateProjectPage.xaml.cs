using TileTextureGenerator.Presentation.UI.Constants;
using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Pages;

/// <summary>
/// Page for creating a new project.
/// Implements responsive layout: horizontal on large/medium screens, vertical on narrow screens.
/// </summary>
public partial class CreateProjectPage : ContentPage
{
    public CreateProjectPage(CreateProjectViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Adjusts layout based on screen width using ScreenBreakpoints constants.
    /// Narrow (<600px): NameStack and OtherStack stacked vertically (1 column)
    /// Medium/Large (≥600px): NameStack and OtherStack side by side (2 columns, 50%/50%)
    /// </summary>
    private void OnResponsiveGridSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Grid grid)
            return;

        var width = grid.Width;

        if (ScreenBreakpoints.IsNarrow(width))
        {
            // Narrow screen: Stack vertically (1 column)
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Microsoft.Maui.Controls.Grid.SetRow(NameStack, 0);
            Microsoft.Maui.Controls.Grid.SetColumn(NameStack, 0);

            Microsoft.Maui.Controls.Grid.SetRow(OtherStack, 1);
            Microsoft.Maui.Controls.Grid.SetColumn(OtherStack, 0);
        }
        else
        {
            // Medium/Large screen: Side by side (2 columns, 50%/50%)
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Microsoft.Maui.Controls.Grid.SetRow(NameStack, 0);
            Microsoft.Maui.Controls.Grid.SetColumn(NameStack, 0);

            Microsoft.Maui.Controls.Grid.SetRow(OtherStack, 0);
            Microsoft.Maui.Controls.Grid.SetColumn(OtherStack, 1);
        }
    }
}
