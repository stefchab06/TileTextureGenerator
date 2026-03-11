using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Transformations;

namespace TileTextureGenerator.Frontend.UI.Controls;

public partial class EdgeFlapConfigControl : ContentView
{
    public EdgeFlapConfigControl()
    {
        InitializeComponent();
        BindingContext = new EdgeFlapConfigViewModel();
    }

    public void SetConfiguration(CardinalDirection direction, EdgeFlapConfiguration config)
    {
        if (BindingContext is EdgeFlapConfigViewModel vm)
        {
            vm.Initialize(direction, config);
        }
    }
}

public partial class EdgeFlapConfigViewModel : ObservableObject
{
    private CardinalDirection _direction;
    private EdgeFlapConfiguration? _config;

    [ObservableProperty]
    private string _directionLabel = string.Empty;

    [ObservableProperty]
    private List<EdgeFlapMode> _modeOptions = Enum.GetValues<EdgeFlapMode>().ToList();

    [ObservableProperty]
    private EdgeFlapMode _selectedMode = EdgeFlapMode.Blank;

    [ObservableProperty]
    private string _colorValue = "#808080";

    [ObservableProperty]
    private string _textureFileName = "No texture selected";

    [ObservableProperty]
    private bool _hasTextureImage;

    [ObservableProperty]
    private ImageSource? _textureImageSource;

    [ObservableProperty]
    private bool _isColorMode;

    [ObservableProperty]
    private bool _isTextureMode;

    public void Initialize(CardinalDirection direction, EdgeFlapConfiguration config)
    {
        _direction = direction;
        _config = config;

        // Localize direction label
        _directionLabel = direction switch
        {
            CardinalDirection.North => TryGetResource("CardinalDirection_North", "North"),
            CardinalDirection.South => TryGetResource("CardinalDirection_South", "South"),
            CardinalDirection.East => TryGetResource("CardinalDirection_East", "East"),
            CardinalDirection.West => TryGetResource("CardinalDirection_West", "West"),
            _ => direction.ToString()
        };

        _selectedMode = config.Mode;
        _colorValue = config.Color ?? "#808080";

        OnPropertyChanged(nameof(DirectionLabel));
        OnPropertyChanged(nameof(SelectedMode));
        OnPropertyChanged(nameof(ColorValue));

        UpdateVisibility();
    }

    private string TryGetResource(string key, string fallback)
    {
        try
        {
            return Resources.Strings.AppResources.ResourceManager.GetString(key) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    partial void OnSelectedModeChanged(EdgeFlapMode oldValue, EdgeFlapMode newValue)
    {
        if (_config != null)
        {
            _config.Mode = newValue;
        }

        UpdateVisibility();
    }

    partial void OnColorValueChanged(string oldValue, string newValue)
    {
        if (_config != null)
        {
            _config.Color = newValue;
        }
    }

    private void UpdateVisibility()
    {
        _isColorMode = _selectedMode == EdgeFlapMode.Color;
        _isTextureMode = _selectedMode == EdgeFlapMode.Texture;

        OnPropertyChanged(nameof(IsColorMode));
        OnPropertyChanged(nameof(IsTextureMode));
    }

    [RelayCommand]
    private async Task SelectTextureAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select a texture image",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                // Read the file
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                if (_config != null)
                {
                    _config.TextureImage = memoryStream.ToArray();
                }

                _textureFileName = result.FileName;
                OnPropertyChanged(nameof(TextureFileName));

                // Set image preview
                _hasTextureImage = true;
                _textureImageSource = ImageSource.FromStream(() => new MemoryStream(_config.TextureImage));
                OnPropertyChanged(nameof(HasTextureImage));
                OnPropertyChanged(nameof(TextureImageSource));
            }
        }
        catch (Exception ex)
        {
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                "Error",
                $"Failed to select image: {ex.Message}",
                "OK");
        }
    }
}
