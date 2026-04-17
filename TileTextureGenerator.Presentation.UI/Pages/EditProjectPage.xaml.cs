using TileTextureGenerator.Presentation.UI.Constants;
using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Pages;

/// <summary>
/// Page for editing a project.
/// Receives EditProjectViewModel from navigation parameters.
/// </summary>
[QueryProperty(nameof(ViewModel), "ViewModel")]
public partial class EditProjectPage : ContentPage
{
    public EditProjectPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ViewModel passed via navigation.
    /// Setting this property assigns it to BindingContext and updates the title.
    /// </summary>
    public EditProjectViewModel? ViewModel
    {
        set
        {
            if (value != null)
            {
                // Set Title BEFORE BindingContext to avoid binding issues
                Title = value.ShellTitle;
                BindingContext = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateTransformationsGridColumns(width);
    }

    private void UpdateTransformationsGridColumns(double width)
    {
        // Use FindByName to avoid compile-time reference
        if (this.FindByName("TransformationsCollectionView") is CollectionView collectionView &&
            collectionView.ItemsLayout is GridItemsLayout gridLayout)
        {
            if (ScreenBreakpoints.IsLarge(width))
                gridLayout.Span = 4;
            else if (ScreenBreakpoints.IsMedium(width))
                gridLayout.Span = 3;
            else
                gridLayout.Span = 1;
        }
    }

    /// <summary>
    /// Navigates back to ManageProjectList when Close is clicked.
    /// </summary>
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
