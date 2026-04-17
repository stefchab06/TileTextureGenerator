using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TileTextureGenerator.Presentation.UI.Controls.ImageCropping;
using TileTextureGenerator.Presentation.UI.Services;

namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// Editing modes for image cropping.
/// </summary>
public enum CroppingMode
{
    Pan,
    Zoom,
    Rotate
}

/// <summary>
/// ViewModel for the image cropping editor page.
/// Handles image acquisition (FilePicker, Clipboard), transformation controls, and validation.
/// </summary>
public class ImageCroppingViewModel : INotifyPropertyChanged
{
    private readonly ImageCroppingService _service;
    private readonly IReadOnlyList<Point> _croppingPolygon;
    private byte[]? _originalImage;
    private byte[]? _currentImage;
    private CroppingTransformation _transformation = CroppingTransformation.Identity;
    private CroppingMode _currentMode = CroppingMode.Pan;
    private bool _hasImage;
    private bool _isLoadingFile; // Prevent multiple simultaneous file pickers
    private Controls.ImageCropping.CroppingCanvasControl? _activeCanvas; // Reference to active canvas

    public ImageCroppingViewModel(
        ImageCroppingService service,
        IReadOnlyList<Point> croppingPolygon,
        byte[]? initialImage)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(croppingPolygon);

        _service = service;
        _croppingPolygon = croppingPolygon;
        _originalImage = initialImage;
        _currentImage = initialImage;
        _hasImage = initialImage != null && initialImage.Length > 0;

        // Commands
        SelectFileCommand = new Command(async () => await SelectFileAsync());
        PasteFromClipboardCommand = new Command(async () => await PasteFromClipboardAsync());
        ValidateCommand = new Command(Validate, () => HasImage);
        CancelCommand = new Command(Cancel);
        SetModeCommand = new Command<CroppingMode>(SetMode);
    }

    /// <summary>
    /// Cropping polygon (proportional coordinates).
    /// </summary>
    public IReadOnlyList<Point> CroppingPolygon => _croppingPolygon;

    /// <summary>
    /// Current image bytes (for display).
    /// </summary>
    public byte[]? CurrentImage
    {
        get => _currentImage;
        private set => SetProperty(ref _currentImage, value);
    }

    /// <summary>
    /// Indicates if an image is loaded.
    /// </summary>
    public bool HasImage
    {
        get => _hasImage;
        private set
        {
            if (SetProperty(ref _hasImage, value))
            {
                ((Command)ValidateCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Current transformation (zoom, pan, rotation).
    /// </summary>
    public CroppingTransformation Transformation
    {
        get => _transformation;
        set => SetProperty(ref _transformation, value);
    }

    /// <summary>
    /// Current editing mode (Pan, Zoom, Rotate).
    /// </summary>
    public CroppingMode CurrentMode
    {
        get => _currentMode;
        private set => SetProperty(ref _currentMode, value);
    }

    /// <summary>
    /// Indicates if Pan mode is active.
    /// </summary>
    public bool IsPanMode => CurrentMode == CroppingMode.Pan;

    /// <summary>
    /// Indicates if Zoom mode is active.
    /// </summary>
    public bool IsZoomMode => CurrentMode == CroppingMode.Zoom;

    /// <summary>
    /// Indicates if Rotate mode is active.
    /// </summary>
    public bool IsRotateMode => CurrentMode == CroppingMode.Rotate;

    public ICommand SelectFileCommand { get; }
    public ICommand PasteFromClipboardCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SetModeCommand { get; }

    /// <summary>
    /// Sets the active canvas control (called by the Page).
    /// </summary>
    public void SetActiveCanvas(Controls.ImageCropping.CroppingCanvasControl? canvas)
    {
        _activeCanvas = canvas;
    }

    private void SetMode(CroppingMode mode)
    {
        CurrentMode = mode;
        OnPropertyChanged(nameof(IsPanMode));
        OnPropertyChanged(nameof(IsZoomMode));
        OnPropertyChanged(nameof(IsRotateMode));
    }

    private async Task SelectFileAsync()
    {
        // Prevent multiple simultaneous file pickers
        if (_isLoadingFile)
            return;

        try
        {
            _isLoadingFile = true;

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select an image",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                _originalImage = memoryStream.ToArray();
                CurrentImage = _originalImage;
                HasImage = true;
                Transformation = CroppingTransformation.Identity; // Reset transformation
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to select file: {ex.Message}");
        }
        finally
        {
            _isLoadingFile = false;
        }
    }

    private async Task PasteFromClipboardAsync()
    {
        // TODO: Clipboard image support (requires platform-specific implementation)
        // For now, show a message that it's not yet implemented
        await Shell.Current.DisplayAlert("Not Implemented", "Clipboard paste will be implemented in a future version.", "OK");
    }

    private void Validate()
    {
        if (!HasImage || _activeCanvas == null)
            return;

        // Get the cropped image from the active canvas
        var croppedImage = _activeCanvas.GetCroppedImage();

        if (croppedImage != null)
        {
            _service.CompleteWithResult(croppedImage);
        }
        else
        {
            // Fallback: return original image if cropping fails
            _service.CompleteWithResult(_currentImage ?? Array.Empty<byte>());
        }

        // Navigate back
        Shell.Current.GoToAsync("..");
    }

    private void Cancel()
    {
        _service.CompleteWithCancellation();

        // Navigate back
        Shell.Current.GoToAsync("..");
    }

    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
