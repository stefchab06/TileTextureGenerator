using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Frontend.UI.ViewModels;

public partial class TransformationsManagementViewModel : ObservableObject
{
    private readonly HorizontalTileTextureProjectEntity _project;

    [ObservableProperty]
    private ObservableCollection<TransformationTypeItem> _availableTypes = new();

    [ObservableProperty]
    private TransformationTypeItem? _selectedType;

    [ObservableProperty]
    private ObservableCollection<TransformationItemViewModel> _transformations = new();

    public TransformationsManagementViewModel(HorizontalTileTextureProjectEntity project)
    {
        _project = project;
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
            var viewModel = new TransformationItemViewModel(transformationEntity, _project);
            _transformations.Add(viewModel);
        }
    }

    [RelayCommand]
    private async Task CreateTransformationAsync()
    {
        if (_selectedType == null)
            return;

        // Open configuration view for the selected transformation type
        if (_selectedType.TypeName == "FlatHorizontalTransformation")
        {
            var configViewModel = new FlatHorizontalTransformationConfigViewModel(_project);
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

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _iconResourcePath = string.Empty;

    [ObservableProperty]
    private byte[]? _generatedImage;

    [ObservableProperty]
    private bool _isGenerated;

    public TransformationItemViewModel(TransformationEntity entity, HorizontalTileTextureProjectEntity project)
    {
        _entity = entity;
        _project = project;

        LoadDisplayInfo();
    }

    private void LoadDisplayInfo()
    {
        // Get display name from the transformation instance
        try
        {
            var transformation = TransformationTypeRegistry.Create(_entity.TransformationType);
            transformation.DeserializeProperties(_entity.Properties);
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
