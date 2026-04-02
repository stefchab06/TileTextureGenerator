using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Ports.Input;

namespace TileTextureGenerator.Adapters.UseCases;

/// <summary>
/// Use case for editing an individual project.
/// Wraps ProjectBase to provide a facade for UI operations.
/// Created by ManageProjectListUseCase after loading/creating a project.
/// </summary>
public class EditProjectUseCase
{
    private readonly ProjectBase _project;

    public EditProjectUseCase(ProjectBase project)
    {
        ArgumentNullException.ThrowIfNull(project);
        _project = project;
    }

    /// <summary>
    /// Exposes the project for UI binding and template selection.
    /// </summary>
    public ProjectBase Project => _project;

    /// <summary>
    /// Gets the project type identifier for UI display.
    /// </summary>
    /// <returns>Type name (e.g., "FloorTileProject").</returns>
    public string GetProjectType() => _project.Type;

    /// <summary>
    /// Gets available transformation types for the current project with their icons.
    /// Returns technical names and icon bytes for UI picker display.
    /// </summary>
    /// <returns>List of tuples (TechnicalName, IconBytes).</returns>
    public async Task<IReadOnlyList<(string TechnicalName, byte[] Icon)>> GetAvailableTransformationTypesAsync()
    {
        var transformationTypes = await _project.GetAvailableTransformationTypesAsync();

        return transformationTypes
            .Select(dto => (dto.Name, dto.Icon?.Bytes ?? Array.Empty<byte>()))
            .ToList();
    }

    /// <summary>
    /// Saves all changes made to the project.
    /// </summary>
    public async Task SaveAsync()
    {
        await _project.SaveChangesAsync();
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

        await _project.AddTransformationAsync(transformationType);
    }

    /// <summary>
    /// Removes a transformation from the project.
    /// </summary>
    /// <param name="transformationId">ID of the transformation to remove.</param>
    public async Task RemoveTransformationAsync(Guid transformationId)
    {
        if (transformationId == Guid.Empty)
            throw new ArgumentException("Transformation ID cannot be empty.", nameof(transformationId));

        await _project.RemoveTransformationAsync(transformationId);
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

        return await _project.GetTransformationAsync(transformationId);
    }

    /// <summary>
    /// Generates all transformations and the final PDF file.
    /// </summary>
    /// <returns>True if generation succeeded.</returns>
    public async Task<bool> GenerateAsync()
    {
        var task = _project.GenerateAsync();
        if (task == null)
            return false;

        return await task;
    }

    /// <summary>
    /// Archives the project (removes workspace, reduces JSON to base properties).
    /// </summary>
    /// <returns>True if archiving succeeded.</returns>
    public async Task<bool> ArchiveAsync()
    {
        return await _project.ArchiveAsync();
    }
}
