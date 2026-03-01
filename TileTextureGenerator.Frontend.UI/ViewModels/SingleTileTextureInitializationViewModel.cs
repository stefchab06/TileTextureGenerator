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
    private int translationPixels = 10;

    [ObservableProperty]
    private bool isToolbarVisible = true;

    [ObservableProperty]
    private bool canPaste = false;

    [ObservableProperty]
    private bool canScan = false;

    public SingleTileTextureInitializationViewModel(IImageSelectionService imageSelectionService)
    {
        _imageSelectionService = imageSelectionService;

        // Hide toolbar on touch devices
        IsToolbarVisible = DeviceInfo.Idiom != DeviceIdiom.Phone && 
                          DeviceInfo.Idiom != DeviceIdiom.Tablet;

        // Check if clipboard and scan are available (platform-specific)
        CheckClipboardAvailability();
        CheckScanAvailability();
    }

    [RelayCommand]
    private async Task LoadImageAsync()
    {
        var imageData = await _imageSelectionService.PickImageFromFileAsync();
        if (imageData != null)
        {
            CurrentImage = imageData;
            ResetTransformations();
        }
    }

    [RelayCommand]
    private async Task PasteImageAsync()
    {
        // TODO: Implement paste from clipboard
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ScanImageAsync()
    {
        // TODO: Implement scan/capture
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void SelectTileShape(TileShape shape)
    {
        SelectedTileShape = shape;
        ResetTransformations();
    }

    [RelayCommand]
    private void RotateClockwise()
    {
        RotationAngle = (RotationAngle + 15) % 360;
    }

    [RelayCommand]
    private void RotateCounterClockwise()
    {
        RotationAngle = (RotationAngle - 15 + 360) % 360;
    }

    [RelayCommand]
    private void MoveUp()
    {
        TranslationY -= TranslationPixels;
    }

    [RelayCommand]
    private void MoveDown()
    {
        TranslationY += TranslationPixels;
    }

    [RelayCommand]
    private void MoveLeft()
    {
        TranslationX -= TranslationPixels;
    }

    [RelayCommand]
    private void MoveRight()
    {
        TranslationX += TranslationPixels;
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        if (CurrentImage == null)
        {
            await Shell.Current.DisplayAlertAsync("Erreur", "Veuillez sélectionner une image", "OK");
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

    private void ResetTransformations()
    {
        TranslationX = 0;
        TranslationY = 0;
        RotationAngle = 0;
        ZoomLevel = 100;
    }

    private async void CheckClipboardAvailability()
    {
        try
        {
            // Clipboard.HasImage is not available in .NET 10 yet
            CanPaste = false; // TODO: Check clipboard when API available
        }
        catch
        {
            CanPaste = false;
        }
    }

    private void CheckScanAvailability()
    {
        // Scan/Camera typically available on mobile
        CanScan = DeviceInfo.Platform == DevicePlatform.Android || 
                  DeviceInfo.Platform == DevicePlatform.iOS;
    }
}
