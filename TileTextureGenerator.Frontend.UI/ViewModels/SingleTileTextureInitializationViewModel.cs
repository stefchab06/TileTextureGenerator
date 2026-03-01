using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Frontend.UI.Services;

namespace TileTextureGenerator.Frontend.UI.ViewModels;

public partial class SingleTileTextureInitializationViewModel : ObservableObject
{
    private readonly IImageSelectionService _imageSelectionService;

    [ObservableProperty]
    private byte[]? currentImage;

    [ObservableProperty]
    private TileShape selectedTileShape = TileShape.Full;

    [ObservableProperty]
    private double translationX = 0;

    [ObservableProperty]
    private double translationY = 0;

    [ObservableProperty]
    private double rotationAngle = 0;

    [ObservableProperty]
    private double zoomLevel = 100;

    [ObservableProperty]
    private double scaleX = 1.0;

    [ObservableProperty]
    private double scaleY = 1.0;

    [ObservableProperty]
    private bool isSnapEnabled = true;

    [ObservableProperty]
    private bool canPaste = false;

    [ObservableProperty]
    private bool canScan = false;

    public SingleTileTextureInitializationViewModel(IImageSelectionService imageSelectionService)
    {
        _imageSelectionService = imageSelectionService;

        // Check if clipboard and scan are available (platform-specific)
        CheckClipboardAvailability();
        CheckScanAvailability();

        // Subscribe to clipboard changes
        _imageSelectionService.OnClipboardChanged(async () =>
        {
            CanPaste = await _imageSelectionService.HasImageInClipboardAsync();
        });
    }

    [RelayCommand]
    private async Task LoadImageAsync()
    {
        var imageData = await _imageSelectionService.PickImageFromFileAsync();
        if (imageData != null)
        {
            CurrentImage = imageData;
        }
    }

    [RelayCommand]
    private async Task PasteImageAsync()
    {
        var imageData = await _imageSelectionService.GetImageFromClipboardAsync();
        if (imageData != null)
        {
            CurrentImage = imageData;
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Information", "No image in clipboard", "OK");
        }
    }

    [RelayCommand]
    private async Task ScanImageAsync()
    {
        // TODO: Implement scan/capture from camera
        await Shell.Current.DisplayAlertAsync("Information", "Scan feature coming soon", "OK");
    }

    [RelayCommand]
    private void SelectTileShape(TileShape shape)
    {
        SelectedTileShape = shape;
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        if (CurrentImage == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please select an image", "OK");
            return;
        }

        // Return the result to the service
        var result = new ImageInitializationResult
        {
            ImageData = CurrentImage,
            TileShape = SelectedTileShape,
            WasCancelled = false
        };

        ImageInitializationService.CompleteInitialization(result);

        // Navigate back
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        // User cancelled
        var result = new ImageInitializationResult
        {
            WasCancelled = true
        };

        ImageInitializationService.CompleteInitialization(result);

        // Navigate back
        await Shell.Current.GoToAsync("..");
    }

    private async void CheckClipboardAvailability()
    {
        CanPaste = await _imageSelectionService.HasImageInClipboardAsync();
    }

    private async void CheckScanAvailability()
    {
        CanScan = await _imageSelectionService.CanScanOrCaptureAsync();
    }
}
