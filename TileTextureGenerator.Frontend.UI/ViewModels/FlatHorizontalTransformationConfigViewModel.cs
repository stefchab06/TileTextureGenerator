using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Transformations.Implementations;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Frontend.UI.ViewModels;

public partial class FlatHorizontalTransformationConfigViewModel : ObservableObject
{
    private readonly HorizontalTileTextureProjectEntity _project;
    private readonly TransformationEntity? _existingEntity;
    private readonly bool _isEdit;

    public FlatHorizontalTransformation Transformation { get; }

    public FlatHorizontalTransformationConfigViewModel(
        HorizontalTileTextureProjectEntity project,
        TransformationEntity? existingEntity = null)
    {
        _project = project;
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
        // Serialize transformation properties
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

        // Navigate back
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
