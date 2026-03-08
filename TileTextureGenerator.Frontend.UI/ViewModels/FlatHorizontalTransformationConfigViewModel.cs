using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TileTextureGenerator.Adapters.Persistence.Dto;
using TileTextureGenerator.Adapters.Persistence.Ports.Output;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Transformations.Implementations;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Frontend.UI.ViewModels;

public partial class FlatHorizontalTransformationConfigViewModel : ObservableObject
{
    private readonly HorizontalTileTextureProjectEntity _project;
    private readonly TransformationEntity? _existingEntity;
    private readonly bool _isEdit;
    private readonly IProjectPersister _projectPersister;

    public FlatHorizontalTransformation Transformation { get; }

    public FlatHorizontalTransformationConfigViewModel(
        HorizontalTileTextureProjectEntity project,
        IProjectPersister projectPersister,
        TransformationEntity? existingEntity = null)
    {
        _project = project;
        _projectPersister = projectPersister;
        _existingEntity = existingEntity;
        _isEdit = existingEntity != null;

        if (_isEdit && _existingEntity != null)
        {
            // Load existing transformation
            Transformation = (FlatHorizontalTransformation)TransformationTypeRegistry.Create(_existingEntity.TransformationType);
            Transformation.DeserializeProperties(_existingEntity.Properties);
            Transformation.Id = _existingEntity.Id;
        }
        else
        {
            // Create new transformation
            Transformation = new FlatHorizontalTransformation
            {
                TileShape = _project.TileShape,
                BaseTexture = _project.SourceImage
            };
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            // Get project folder path (using AppData location)
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var projectFolder = Path.Combine(appData, "TileTextureGenerator", _project.Name);

            // Save EdgeFlap textures to workspace and store paths in EdgeFlaps.Texture
            await SaveEdgeFlapTextureAsync(CardinalDirection.North, projectFolder);
            await SaveEdgeFlapTextureAsync(CardinalDirection.South, projectFolder);
            await SaveEdgeFlapTextureAsync(CardinalDirection.East, projectFolder);
            await SaveEdgeFlapTextureAsync(CardinalDirection.West, projectFolder);

            // Debug: Verify Texture paths are set
            System.Diagnostics.Debug.WriteLine($"North: Mode={Transformation.EdgeFlaps.North.Mode}, Texture={Transformation.EdgeFlaps.North.Texture ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"South: Mode={Transformation.EdgeFlaps.South.Mode}, Color={Transformation.EdgeFlaps.South.Color ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"East: Mode={Transformation.EdgeFlaps.East.Mode}");
            System.Diagnostics.Debug.WriteLine($"West: Mode={Transformation.EdgeFlaps.West.Mode}, Texture={Transformation.EdgeFlaps.West.Texture ?? "null"}");

            // Serialize transformation properties (includes EdgeFlaps with Texture paths)
            var properties = Transformation.SerializeProperties();

            if (_isEdit && _existingEntity != null)
            {
                // Update existing
                _existingEntity.Properties = properties;
                _existingEntity.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new
                var entity = new TransformationEntity
                {
                    Id = Transformation.Id,
                    TransformationType = nameof(FlatHorizontalTransformation),
                    DisplayOrder = _project.Transformations.Count,
                    Properties = properties
                };

                _project.Transformations.Add(entity);
            }

            // Save project to filesystem
            var projectDto = new ProjectDataDto(
                _project.Name,
                _project.Type,
                _project.Status.ToString(),
                _project.ToJson())
            {
                DisplayImage = _project.DisplayImage,
                SourceImage = _project.SourceImage
            };

            await _projectPersister.SaveProjectAsync(projectDto);

            // Navigate back
            await Application.Current!.MainPage!.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                $"Failed to save: {ex.Message}",
                "OK");
        }
    }

    private async Task SaveEdgeFlapTextureAsync(
        CardinalDirection direction,
        string projectFolder)
    {
        var config = Transformation.EdgeFlaps[direction];

        if (config.Mode == Core.Enums.EdgeFlapMode.Texture && config.TextureImage != null && config.TextureImage.Length > 0)
        {
            // Check if we already have a texture path (reuse existing file)
            string filename;
            if (!string.IsNullOrEmpty(config.Texture))
            {
                // Extract filename from existing path (e.g., "Workspace\guid.png" -> "guid.png")
                filename = Path.GetFileName(config.Texture);
            }
            else
            {
                // Generate new GUID filename
                filename = $"{Guid.NewGuid()}.png";
            }

            // Save to Workspace folder
            var workspaceDir = Path.Combine(projectFolder, "Workspace");
            Directory.CreateDirectory(workspaceDir);
            var fullPath = Path.Combine(workspaceDir, filename);
            await File.WriteAllBytesAsync(fullPath, config.TextureImage);

            // Store relative path in config
            config.Texture = Path.Combine("Workspace", filename);
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
