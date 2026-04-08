using System.Text.Json;
using System.Text.Json.Nodes;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Ports.Input;

namespace TileTextureGenerator.Adapters.UseCases;

/// <summary>
/// Use case for editing an individual project.
/// Wraps ProjectBase to provide a facade for UI operations.
/// Created by ManageProjectListUseCase after loading/creating a project.
/// Exposes project properties as JSON to decouple UI from Core entities.
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
    /// Exposes the project for UI binding (base properties only: Name, Type, Status).
    /// </summary>
    public ProjectBase Project => _project;

    /// <summary>
    /// Gets the concrete type name of the project (for template selection).
    /// </summary>
    public string ConcreteTypeName => _project.GetType().Name;

    /// <summary>
    /// Gets the concrete project properties as JSON (for UI binding).
    /// Excludes base properties (Name, Type, Status, etc.).
    /// Enums are serialized as strings for readability.
    /// </summary>
    public JsonObject GetPropertiesJson()
    {
        // Serialize the entire project with enums as strings
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = false,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(_project, _project.GetType(), options);

        // Parse as JsonObject
        var jsonObject = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        // Remove base properties (inherited from ProjectBase)
        jsonObject.Remove("Name");
        jsonObject.Remove("Type");
        jsonObject.Remove("Status");
        jsonObject.Remove("DisplayImage");
        jsonObject.Remove("Transformations");

        return jsonObject;
    }

    /// <summary>
    /// Updates the concrete project properties from JSON.
    /// Copies properties individually to avoid deserialization issues with parameterized constructors.
    /// </summary>
    /// <param name="propertiesJson">JSON object containing updated properties.</param>
    public void UpdatePropertiesFromJson(JsonObject propertiesJson)
    {
        ArgumentNullException.ThrowIfNull(propertiesJson);

        // Serialize options with enum as string
        var options = new JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        // Update properties individually (avoid full deserialization due to constructor issues)
        foreach (var (key, value) in propertiesJson)
        {
            var property = _project.GetType().GetProperty(key);
            if (property != null && property.CanWrite)
            {
                try
                {
                    // Deserialize the value to the correct type
                    var targetType = property.PropertyType;
                    var jsonValue = value?.ToJsonString() ?? "null";
                    var deserializedValue = JsonSerializer.Deserialize(jsonValue, targetType, options);

                    // Set the property
                    property.SetValue(_project, deserializedValue);
                }
                catch
                {
                    // Skip properties that fail to deserialize
                    continue;
                }
            }
        }
    }

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
