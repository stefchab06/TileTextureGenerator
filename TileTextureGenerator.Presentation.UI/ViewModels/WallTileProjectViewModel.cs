using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Windows.Input;
using TileTextureGenerator.Presentation.UI.Services;

namespace TileTextureGenerator.Presentation.UI.ViewModels;

/// <summary>
/// ViewModel wrapper for WallTileProject.
/// Provides observable properties and UI commands.
/// Works with JSON properties instead of direct entity reference.
/// </summary>
public class WallTileProjectViewModel : INotifyPropertyChanged
{
    private readonly JsonObject _propertiesJson;
    private readonly TileShapeLocalizer _tileShapeLocalizer;
    private TileShapeItem? _selectedTileShape;
    private bool _isTileShapePickerExpanded;
    private bool _isSelectingImage;

    /// <summary>
    /// Constructor with unified signature for all template ViewModels.
    /// </summary>
    /// <param name="propertiesJson">Shared JSON object (modifications reflected in parent)</param>
    /// <param name="parentViewModel">Parent EditProjectViewModel (provides services)</param>
    public WallTileProjectViewModel(JsonObject propertiesJson, EditProjectViewModel parentViewModel)
    {
        ArgumentNullException.ThrowIfNull(propertiesJson);
        ArgumentNullException.ThrowIfNull(parentViewModel);

        _propertiesJson = propertiesJson;
        _tileShapeLocalizer = parentViewModel.TileShapeLocalizer;

        // Initialize available tile shapes
        AvailableTileShapes = new ObservableCollection<TileShapeItem>(
            _tileShapeLocalizer.GetAllTileShapes()
        );

        // Load TileShape from JSON (string value like "Full", "HalfHorizontal")
        var tileShapeValue = _propertiesJson["TileShape"]?.GetValue<string>() ?? "Full";
        _selectedTileShape = AvailableTileShapes.FirstOrDefault(x => x.Value == tileShapeValue);

        // Commands
        ToggleTileShapePickerCommand = new Command(ToggleTileShapePicker);
        SelectTileShapeCommand = new Command<TileShapeItem>(OnTileShapeSelected);
        SelectSourceImageCommand = new Command(async () => await SelectSourceImageAsync());
    }

    /// <summary>
    /// Available tile shapes for picker.
    /// </summary>
    public ObservableCollection<TileShapeItem> AvailableTileShapes { get; }

    /// <summary>
    /// Selected tile shape.
    /// </summary>
    public TileShapeItem? SelectedTileShape
    {
        get => _selectedTileShape;
        set
        {
            if (SetProperty(ref _selectedTileShape, value))
            {
                // Update JSON (store string value like "Full")
                if (value != null)
                {
                    _propertiesJson["TileShape"] = value.Value;
                }
            }
        }
    }

    /// <summary>
    /// Indicates if the tile shape picker is expanded.
    /// </summary>
    public bool IsTileShapePickerExpanded
    {
        get => _isTileShapePickerExpanded;
        set => SetProperty(ref _isTileShapePickerExpanded, value);
    }

    /// <summary>
    /// Source image bytes for display.
    /// JSON stores bytes as base64 string (System.Text.Json default).
    /// </summary>
    public byte[]? SourceImageBytes
    {
        get
        {
            var imageData = _propertiesJson["SourceImage"]?.AsObject();
            if (imageData == null)
                return null;

            // Bytes are stored as base64 string in JSON
            var bytesBase64 = imageData["Bytes"]?.GetValue<string>();
            if (string.IsNullOrEmpty(bytesBase64))
                return null;

            try
            {
                return Convert.FromBase64String(bytesBase64);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Indicates if source image is loaded.
    /// </summary>
    public bool HasSourceImage
    {
        get
        {
            var bytes = SourceImageBytes;
            return bytes != null && bytes.Length > 0;
        }
    }

    public ICommand ToggleTileShapePickerCommand { get; }
    public ICommand SelectTileShapeCommand { get; }
    public ICommand SelectSourceImageCommand { get; }

    private void ToggleTileShapePicker()
    {
        IsTileShapePickerExpanded = !IsTileShapePickerExpanded;
    }

    private void OnTileShapeSelected(TileShapeItem item)
    {
        SelectedTileShape = item;
        IsTileShapePickerExpanded = false;
    }

    private async Task SelectSourceImageAsync()
    {
        // Prevent multiple simultaneous file pickers
        if (_isSelectingImage)
            return;

        try
        {
            _isSelectingImage = true;

            // Use MAUI FilePicker to select an image
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select source image",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                // Read image bytes
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                // Update JSON (store as base64 string, System.Text.Json default for byte[])
                var imageData = new JsonObject
                {
                    ["Bytes"] = Convert.ToBase64String(bytes)
                };
                _propertiesJson["SourceImage"] = imageData;

                // Notify UI
                OnPropertyChanged(nameof(SourceImageBytes));
                OnPropertyChanged(nameof(HasSourceImage));
            }
        }
        catch (Exception ex)
        {
            // TODO: Error handling (future iteration)
            System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
        }
        finally
        {
            _isSelectingImage = false;
        }
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
