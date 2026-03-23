using System.Text.Json;
using System.Text.Json.Nodes;
using TileTextureGenerator.Adapters.Persistence.Ports;

namespace TileTextureGenerator.Adapters.Persistence.Utilities;

/// <summary>
/// Helper class for loading and saving project JSON files.
/// Centralizes common JSON file operations used across store implementations.
/// </summary>
public sealed class ProjectJsonHelper
{
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// Initializes a new instance of ProjectJsonHelper.
    /// </summary>
    /// <param name="fileStorage">File storage abstraction for I/O operations.</param>
    public ProjectJsonHelper(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    /// <summary>
    /// Loads the JSON document for a project.
    /// Returns an empty JsonObject if the file doesn't exist or is empty.
    /// </summary>
    /// <param name="projectName">The name of the project.</param>
    /// <returns>The JsonObject representing the project, or empty JsonObject if not found.</returns>
    public async Task<JsonObject> LoadProjectJsonAsync(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);

        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(projectName));

        string cleanedName = FileNameHelper.CleanFileName(projectName);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);

        if (!await _fileStorage.FileExistsAsync(jsonPath))
            return new JsonObject();

        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        
        return string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }

    /// <summary>
    /// Saves the JSON document for a project.
    /// </summary>
    /// <param name="projectName">The name of the project.</param>
    /// <param name="jsonDoc">The JsonObject to save.</param>
    /// <param name="options">JSON serialization options.</param>
    public async Task SaveProjectJsonAsync(string projectName, JsonObject jsonDoc, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(projectName);
        ArgumentNullException.ThrowIfNull(jsonDoc);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(projectName));

        string cleanedName = FileNameHelper.CleanFileName(projectName);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);

        string jsonContent = jsonDoc.ToJsonString(options);
        await _fileStorage.WriteAllTextAsync(jsonPath, jsonContent);
    }
}
