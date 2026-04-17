using TileTextureGenerator.Presentation.UI.Services;
using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Pages;

/// <summary>
/// Page for image cropping with polygon mask.
/// Retrieves parameters from ImageCroppingService on navigation.
/// Adapts layout based on orientation (portrait vs landscape).
/// </summary>
public partial class ImageCroppingPage : ContentPage
{
    private readonly ImageCroppingService _service;
    private ImageCroppingViewModel? _viewModel;

    public ImageCroppingPage(ImageCroppingService service)
    {
        InitializeComponent();
        _service = service;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Retrieve parameters from service
        var croppingPolygon = _service.GetCurrentCroppingPolygon();
        var initialImage = _service.GetCurrentInitialImage();

        if (croppingPolygon != null)
        {
            // Create and set ViewModel
            _viewModel = new ImageCroppingViewModel(_service, croppingPolygon, initialImage);
            BindingContext = _viewModel;

            // Subscribe to image changes
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Initialize canvas with image and polygon
            UpdateCanvas();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // If page is closed/navigated away without explicit validation/cancellation,
        // complete with cancellation to unblock the calling code
        _service.CompleteWithCancellation();

        // Unsubscribe
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update canvas when image or transformation changes
        if (e.PropertyName == nameof(ImageCroppingViewModel.CurrentImage) ||
            e.PropertyName == nameof(ImageCroppingViewModel.Transformation))
        {
            UpdateCanvas();
        }
    }

    private void UpdateCanvas()
    {
        if (_viewModel == null)
            return;

        // Find the canvas controls
        var portraitCanvas = this.FindByName<Controls.ImageCropping.CroppingCanvasControl>("PortraitCanvas");
        var landscapeCanvas = this.FindByName<Controls.ImageCropping.CroppingCanvasControl>("LandscapeCanvas");

        // Store reference to active canvas in ViewModel
        var activeCanvas = portraitCanvas?.IsVisible == true ? portraitCanvas : landscapeCanvas;
        _viewModel.SetActiveCanvas(activeCanvas);

        // Update both canvases (only one is visible at a time)
        if (portraitCanvas != null)
        {
            portraitCanvas.SetImage(_viewModel.CurrentImage, _viewModel.CroppingPolygon);
            portraitCanvas.SetTransformation(_viewModel.Transformation);
        }

        if (landscapeCanvas != null)
        {
            landscapeCanvas.SetImage(_viewModel.CurrentImage, _viewModel.CroppingPolygon);
            landscapeCanvas.SetTransformation(_viewModel.Transformation);
        }
    }

    /// <summary>
    /// Handles size changes to adapt layout based on orientation.
    /// </summary>
    private void OnRootSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Grid rootGrid)
            return;

        if (rootGrid.Width <= 0 || rootGrid.Height <= 0)
            return;

        // Determine orientation
        bool isPortrait = rootGrid.Height > rootGrid.Width;

        // Find layouts
        var portraitLayout = this.FindByName<Grid>("PortraitLayout");
        var landscapeLayout = this.FindByName<Grid>("LandscapeLayout");

        // Switch layouts
        if (portraitLayout != null)
            portraitLayout.IsVisible = isPortrait;

        if (landscapeLayout != null)
            landscapeLayout.IsVisible = !isPortrait;
    }

    /// <summary>
    /// Called when user presses hardware/system back button.
    /// Cancels the cropping operation and navigates back.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        // Cancel the operation (will be handled by OnDisappearing)
        _service.CompleteWithCancellation();

        // Navigate back
        Task.Run(async () => await Shell.Current.GoToAsync(".."));

        return true; // Prevent default back navigation
    }
}
