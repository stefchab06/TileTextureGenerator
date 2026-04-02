using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Ports.Input;

namespace TileTextureGenerator.Adapters.UseCases;

/// <summary>
/// Use case for editing an individual project.
/// Wraps IProjectManager to provide a facade for UI operations.
/// Created by ManageProjectListUseCase after loading/creating a project.
/// </summary>
public class EditProjectUseCase
{
    private readonly IProjectManager _projectManager;

    public EditProjectUseCase(IProjectManager projectManager)
    {
        ArgumentNullException.ThrowIfNull(projectManager);
        _projectManager = projectManager;
    }

    /// <summary>
    /// Gets the project type identifier for UI template selection.
    /// </summary>
    /// <returns>Type name (e.g., "FloorTileProject").</returns>
    public string GetProjectType() => _projectManager.Type;

    /// <summary>
    /// Saves all changes made to the project.
    /// </summary>
    public async Task SaveAsync()
    {
        await _projectManager.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a new transformation to the project.
    /// </summary>
    /// <param name="transformationType">Type identifier of the transformation (e.g., "HorizontalFloorTransformation").</param>
    public async Task AddTransformationAsync(string transformationType)
    {
        ArgumentNullException.ThrowIfNull(transformationType);
        if (string.IsNullOrWhiteSpace(transformationType))
            throw new ArgumentException("Transformation type cannot be empty or whitespace.", nameof(transformationType));

        await _projectManager.AddTransformationAsync(transformationType);
    }

    /// <summary>
    /// Removes a transformation from the project.
    /// </summary>
    /// <param name="transformationId">ID of the transformation to remove.</param>
    public async Task RemoveTransformationAsync(Guid transformationId)
    {
        if (transformationId == Guid.Empty)
            throw new ArgumentException("Transformation ID cannot be empty.", nameof(transformationId));

        await _projectManager.RemoveTransformationAsync(transformationId);
    }

    /// <summary>
    /// Gets a transformation instance by ID.
    /// </summary>
    /// <param name="transformationId">ID of the transformation to retrieve.</param>
    /// <returns>The transformation instance.</returns>
    public async Task<TransformationBase> GetTransformationAsync(Guid transformationId)
    {
        if (transformationId == Guid.Empty)
            throw new ArgumentException("Transformation ID cannot be empty.", nameof(transformationId));

        return await _projectManager.GetTransformationAsync(transformationId);
    }

    /// <summary>
    /// Gets the list of transformation types available for the current project type.
    /// </summary>
    /// <returns>List of available transformation types with metadata (name and icon).</returns>
    public async Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
    {
        return await _projectManager.GetAvailableTransformationTypesAsync();
    }

    /// <summary>
    /// Generates all transformations and the final PDF file.
    /// </summary>
    /// <returns>True if generation succeeded.</returns>
    public async Task<bool> GenerateAsync()
    {
        return await _projectManager.GenerateAsync();
    }

    /// <summary>
    /// Archives the project (removes workspace, reduces JSON to base properties).
    /// </summary>
    /// <returns>True if archiving succeeded.</returns>
    public async Task<bool> ArchiveAsync()
    {
        return await _projectManager.ArchiveAsync();
    }
}
