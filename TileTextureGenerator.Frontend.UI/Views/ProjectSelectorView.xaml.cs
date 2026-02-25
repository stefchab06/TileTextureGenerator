using TileTextureGenerator.Frontend.UI.ViewModels;
using TileTextureGenerator.Frontend.UI.Views;
namespace TileTextureGenerator.Frontend.UI.Views;

public partial class ProjectSelectorView : ContentPage
{
	public ProjectSelectorView(ProjectSelectorViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        this.SizeChanged += OnPageSizeChanged;

        Loaded += async (_, __) => await vm.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.SizeChanged -= OnPageSizeChanged;
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (ProjectsCollection?.ItemsLayout is GridItemsLayout gridLayout)
        {
            gridLayout.Span = Width switch
            {
                >= 900 => 3,  // 3 columns on large screens
                >= 600 => 2,  // 2 columns on medium screens
                _ => 1        // 1 column on small screens
            };
        }
    }
}