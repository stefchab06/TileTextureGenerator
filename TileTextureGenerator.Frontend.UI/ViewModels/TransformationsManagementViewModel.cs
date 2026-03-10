using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TileTextureGenerator.Adapters.Persistence.Ports.Output;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Frontend.UI.ViewModels;

public partial class TransformationsManagementViewModel : ObservableObject
{
    private readonly HorizontalTileTextureProjectEntity _project;
    private readonly IProjectPersister _projectPersister;
    private readonly string _projectFolder;

    [ObservableProperty]
    private ObservableCollection<TransformationTypeItem> _availableTypes = new();

    [ObservableProperty]
    private TransformationTypeItem? _selectedType;

    [ObservableProperty]
    private ObservableCollection<TransformationItemViewModel> _transformations = new();

    public TransformationsManagementViewModel(
        HorizontalTileTextureProjectEntity project,
        IProjectPersister projectPersister)
    {
        _project = project;
        _projectPersister = projectPersister;

        // Calculate project folder once
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _projectFolder = Path.Combine(appData, "TileTextureGenerator", _project.Name);

        LoadAvailableTypes();
        LoadTransformations();
    }

    private void LoadAvailableTypes()
    {
        _availableTypes.Clear();

        var availableTypes = TransformationTypeRegistry.GetAvailableTypes(_project.Transformations);

        foreach (var type in availableTypes)
        {
            var iconPath = TransformationTypeRegistry.GetIconResourcePath(type);

            // Get localized name or fallback
            string displayName = type.Name switch
            {
                "FlatHorizontalTransformation" => TryGetResource("TransformationType_FlatHorizontal", "Flat Horizontal"),
                _ => type.Name.Replace("Transformation", "")
            };

            var item = new TransformationTypeItem
            {
                Type = type,
                TypeName = type.Name,
                DisplayName = displayName,
                IconResourcePath = iconPath
            };

            _availableTypes.Add(item);
        }

        SelectedType = _availableTypes.FirstOrDefault();
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

    private void LoadTransformations()
    {
        _transformations.Clear();

        foreach (var transformationEntity in _project.Transformations.OrderBy(t => t.DisplayOrder))
        {
            var viewModel = new TransformationItemViewModel(transformationEntity, _project, _projectFolder);
            _transformations.Add(viewModel);
        }
    }

    [RelayCommand]
    private async Task EditTransformationAsync(TransformationItemViewModel item)
    {
        // Find the corresponding entity
        var entity = _project.Transformations.FirstOrDefault(t => t.Id == item.Entity.Id);
        if (entity == null)
            return;

        // Hydrate the transformation with textures from disk
        var hydratedTransformation = await HydrateTransformationWithTexturesAsync(entity);

        // Open configuration view based on transformation type
        if (entity.TransformationType == "FlatHorizontalTransformation")
        {
            var configViewModel = new FlatHorizontalTransformationConfigViewModel(
                _project,
                _projectPersister,
                entity,
                hydratedTransformation); // Pass the hydrated transformation
            var configView = new TileTextureGenerator.Frontend.UI.Views.FlatHorizontalTransformationConfigView(configViewModel);
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.Navigation.PushAsync(configView);
        }
        else
        {
            // TODO: Handle other transformation types
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                "Not Implemented",
                $"Edit for {entity.TransformationType} not yet implemented",
                "OK");
        }

        // Refresh list when returning
        LoadAvailableTypes();
        LoadTransformations();
    }

    private async Task<Core.Transformations.TransformationBase> HydrateTransformationWithTexturesAsync(TransformationEntity entity)
    {
        // Create transformation instance and deserialize
        var transformation = TransformationTypeRegistry.Create(entity.TransformationType);
        transformation.DeserializeProperties(entity.Properties);

        // Load textures from disk
        var directions = new[] {
            Core.Enums.CardinalDirection.North,
            Core.Enums.CardinalDirection.South,
            Core.Enums.CardinalDirection.East,
            Core.Enums.CardinalDirection.West
        };

        foreach (var direction in directions)
        {
            var config = transformation.EdgeFlaps[direction];

            if (config.Mode == Core.Enums.EdgeFlapMode.Texture && !string.IsNullOrEmpty(config.Texture))
            {
                try
                {
                    var fullPath = Path.Combine(_projectFolder, config.Texture);
                    System.Diagnostics.Debug.WriteLine($"[Hydrate] Loading {direction}: {fullPath}");

                    if (File.Exists(fullPath))
                    {
                        var imageData = await File.ReadAllBytesAsync(fullPath);
                        config.TextureImage = imageData;
                        System.Diagnostics.Debug.WriteLine($"[Hydrate] Loaded {direction}: {imageData.Length} bytes");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Hydrate] File not found: {fullPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Hydrate] Error loading {direction}: {ex.Message}");
                }
            }
        }

        // Return the hydrated transformation (don't re-serialize - would lose TextureImage!)
        return transformation;
    }

    [RelayCommand]
    private async Task CreateTransformationAsync()
    {
        if (_selectedType == null)
            return;

        // Open configuration view for the selected transformation type
        if (_selectedType.TypeName == "FlatHorizontalTransformation")
        {
            var configViewModel = new FlatHorizontalTransformationConfigViewModel(
                _project,
                _projectPersister);
            var configView = new TileTextureGenerator.Frontend.UI.Views.FlatHorizontalTransformationConfigView(configViewModel);
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.Navigation.PushAsync(configView);
        }
        else
        {
            // TODO: Handle other transformation types
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                "Not Implemented",
                $"Configuration for {_selectedType.DisplayName} not yet implemented",
                "OK");
        }

        // Refresh list when returning
        LoadAvailableTypes();
        LoadTransformations();
    }

    public void Refresh()
    {
        LoadAvailableTypes();
        LoadTransformations();
    }
}

/// <summary>
/// Represents a transformation type that can be created
/// </summary>
public class TransformationTypeItem
{
    public Type Type { get; set; } = null!;
    public string TypeName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string IconResourcePath { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for a single transformation in the list
/// </summary>
public partial class TransformationItemViewModel : ObservableObject
{
    private readonly TransformationEntity _entity;
    private readonly HorizontalTileTextureProjectEntity _project;
    private readonly string _projectFolder;

    public TransformationEntity Entity => _entity; // Expose for edit command

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _iconResourcePath = string.Empty;

    [ObservableProperty]
    private byte[]? _generatedImage;

    [ObservableProperty]
    private bool _isGenerated;

    public TransformationItemViewModel(
        TransformationEntity entity,
        HorizontalTileTextureProjectEntity project,
        string projectFolder)
    {
        _entity = entity;
        _project = project;
        _projectFolder = projectFolder;

        LoadDisplayInfo();
    }

    private void LoadDisplayInfo()
    {
        // Get display name from the transformation instance
        try
        {
            var transformation = TransformationTypeRegistry.Create(_entity.TransformationType);
            transformation.DeserializeProperties(_entity.Properties);

            // Load EdgeFlap textures from workspace
            LoadEdgeFlapTextures(transformation);

            _displayName = transformation.GetDisplayName();
            _iconResourcePath = transformation.GetIconResourcePath();
        }
        catch
        {
            _displayName = _entity.TransformationType;
            _iconResourcePath = string.Empty;
        }

        _isGenerated = _entity.IsGenerated;

        // TODO: Load generated image if exists
    }

    private async void LoadEdgeFlapTextures(Core.Transformations.TransformationBase transformation)
    {
        var directions = new[] {
            Core.Enums.CardinalDirection.North,
            Core.Enums.CardinalDirection.South,
            Core.Enums.CardinalDirection.East,
            Core.Enums.CardinalDirection.West
        };

        foreach (var direction in directions)
        {
            var config = transformation.EdgeFlaps[direction];

            if (config.Mode == Core.Enums.EdgeFlapMode.Texture && !string.IsNullOrEmpty(config.Texture))
            {
                try
                {
                    // Texture path is relative to project folder (e.g., "Workspace\guid.png")
                    var fullPath = Path.Combine(_projectFolder, config.Texture);

                    if (File.Exists(fullPath))
                    {
                        var imageData = await File.ReadAllBytesAsync(fullPath);
                        config.TextureImage = imageData;
                    }
                }
                catch
                {
                    // Ignore errors loading individual textures
                }
            }
        }
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        // TODO: Open configuration view for editing
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            // Create transformation instance
            var transformation = TransformationTypeRegistry.Create(_entity.TransformationType);
            transformation.DeserializeProperties(_entity.Properties);

            // Set BaseTexture from project
            if (transformation is TileTextureGenerator.Core.Transformations.Implementations.FlatHorizontalTransformation flatTransform)
            {
                flatTransform.BaseTexture = _project.SourceImage;
                flatTransform.TileShape = _project.TileShape;
            }

            // Create project context
            var context = new TileTextureGenerator.Core.Transformations.ProjectContext
            {
                ProjectId = Guid.NewGuid(), // TODO: Get actual project ID when available
                SourceImage = _project.SourceImage ?? Array.Empty<byte>(),
                TileShape = _project.TileShape
            };

            // Execute transformation
            var result = await transformation.ExecuteAsync(context);

            if (result.Success && result.OutputImage != null)
            {
                // Update entity
                _entity.IsGenerated = true;
                _entity.LastGeneratedDate = DateTime.UtcNow;

                // Store generated image for display
                _generatedImage = result.OutputImage;
                _isGenerated = true;

                // Trigger property change notifications
                OnPropertyChanged(nameof(GeneratedImage));
                OnPropertyChanged(nameof(IsGenerated));

                await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                    "Success",
                    "Transformation generated successfully!",
                    "OK");
            }
            else
            {
                await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    $"Failed to generate transformation: {result.ErrorMessage}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                "Error",
                $"Exception: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        _project.Transformations.Remove(_entity);
        // TODO: Refresh parent list
        await Task.CompletedTask;
    }
}
