using System.Text.Json;
using System.Text.Json.Serialization;
using TileTextureGenerator.Adapters.Persistence.Converters;
using TileTextureGenerator.Adapters.Persistence.Ports;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Adapters.Persistence.Stores;

/// <summary>
/// JSON-based implementation of IProjectsStore and IProjectStore.
/// Stores projects as JSON files in a directory structure with separate folders for images.
/// Uses polymorphic serialization to handle concrete project types.
/// </summary>
public sealed class JsonProjectsStore : IProjectsStore
{
    private readonly IFileStorage _fileStorage;
    private readonly ImagePersistenceHelper _imageHelper;
    private readonly string _projectsBasePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = 
        { 
            new JsonStringEnumConverter(),
            new ImageDataJsonConverter(),
            new NullableImageDataJsonConverter()
        }
    };

    /// <summary>
    /// Initializes a new instance of JsonProjectsStore.
    /// </summary>
    /// <param name="fileStorage">File storage abstraction for I/O operations.</param>
    public JsonProjectsStore(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _imageHelper = new ImagePersistenceHelper(fileStorage);

        _projectsBasePath = _fileStorage.GetProjectsRootPath();
    }

    /// <inheritdoc />
    public async Task CreateProjectAsync(ProjectDto projectDto)
    {
        ArgumentNullException.ThrowIfNull(projectDto);

        // Clean the project name for directory/file naming
        string cleanedName = FileNameHelper.CleanFileName(projectDto.Name);
        string projectDir = Path.Combine(_projectsBasePath, cleanedName);
        string jsonPath = Path.Combine(projectDir, $"{cleanedName}.json");

        // Check for name conflict
        await CheckForNameConflictAsync(projectDir, jsonPath, projectDto.Name);

        // Create directory structure
        await _fileStorage.EnsureDirectoryExistsAsync(projectDir);
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Sources"));
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Workspace"));
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Outputs"));

        // Save DisplayImage if present and track its path
        string? displayImagePath = null;
        if (projectDto.DisplayImage.HasValue && projectDto.DisplayImage.Value.Bytes.Length > 0)
        {
            await _imageHelper.SavePropertyImageAsync(
                projectDto.DisplayImage.Value.Bytes,
                projectDir,
                "Sources",
                "DisplayImage"
            );
            displayImagePath = "Sources/DisplayImage.png";
        }

        // Serialize DTO to JSON
        string jsonContent = JsonSerializer.Serialize(projectDto, JsonOptions);

        // Add image path if present
        if (displayImagePath != null)
        {
            var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, JsonOptions);
            if (jsonDoc != null)
            {
                jsonDoc["displayImagePath"] = JsonSerializer.SerializeToElement(displayImagePath, JsonOptions);
                jsonContent = JsonSerializer.Serialize(jsonDoc, JsonOptions);
            }
        }

        await _fileStorage.WriteAllTextAsync(jsonPath, jsonContent);
    }

    /// <inheritdoc />
    public async Task<ProjectBase?> LoadAsync(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);

        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(projectName));

        string cleanedName = FileNameHelper.CleanFileName(projectName);

        string projectDir = Path.Combine(_projectsBasePath, cleanedName);
        string jsonPath = Path.Combine(projectDir, $"{cleanedName}.json");

        if (!await _fileStorage.FileExistsAsync(jsonPath))
            return null;

        // Read and deserialize JSON
        string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
        var projectData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, JsonOptions);

        if (projectData == null)
            return null;

        // Extract type and name
        string? typeName = projectData.TryGetValue("type", out var typeElement) 
            ? typeElement.GetString() 
            : null;

        string? realName = projectData.TryGetValue("name", out var nameElement) 
            ? nameElement.GetString() 
            : null;

        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(realName))
            return null;

        // Verify name matches
        if (realName != projectName)
            return null;

        // Create instance using registry
        ProjectBase project = TextureProjectRegistry.Create(typeName, realName);

        // Deserialize all properties (polymorphic)
        DeserializeProjectProperties(project, projectData, projectDir);

        // Load images (using paths from JSON)
        await LoadProjectImagesAsync(project, projectDir, projectData);

        return project;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);
        
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(projectName));

        string cleanedName = FileNameHelper.CleanFileName(projectName);
        string projectDir = Path.Combine(_projectsBasePath, cleanedName);

        if (await _fileStorage.DirectoryExistsAsync(projectDir))
        {
            await _fileStorage.DeleteDirectoryAsync(projectDir, recursive: true);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);
        
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(projectName));

        string cleanedName = FileNameHelper.CleanFileName(projectName);
        string projectDir = Path.Combine(_projectsBasePath, cleanedName);
        string jsonPath = Path.Combine(projectDir, $"{cleanedName}.json");

        return await _fileStorage.DirectoryExistsAsync(projectDir) 
            && await _fileStorage.FileExistsAsync(jsonPath);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectDto>> ListProjectsAsync()
    {
        if (!await _fileStorage.DirectoryExistsAsync(_projectsBasePath))
            return Array.Empty<ProjectDto>();

        var projects = new List<ProjectDto>();
        var directories = await _fileStorage.GetDirectoriesAsync(_projectsBasePath);

        foreach (string dir in directories)
        {
            string dirName = Path.GetFileName(dir);
            string jsonPath = Path.Combine(dir, $"{dirName}.json");

            if (!await _fileStorage.FileExistsAsync(jsonPath))
                continue;

            try
            {
                string jsonContent = await _fileStorage.ReadAllTextAsync(jsonPath);
                var projectData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, JsonOptions);

                if (projectData == null)
                    continue;

                // Extract basic properties for DTO
                string? name = projectData.TryGetValue("name", out var nameEl) ? nameEl.GetString() : null;
                string? type = projectData.TryGetValue("type", out var typeEl) ? typeEl.GetString() : null;
                var status = projectData.TryGetValue("status", out var statusEl) 
                    ? statusEl.Deserialize<Core.Enums.ProjectStatus>(JsonOptions) 
                    : Core.Enums.ProjectStatus.Unexisting;
                var lastModified = projectData.TryGetValue("lastModifiedDate", out var dateEl) 
                    ? dateEl.GetDateTime() 
                    : DateTime.UtcNow;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
                {
                    var dto = new ProjectDto(name, type, status, lastModified);
                    projects.Add(dto);
                }
            }
            catch
            {
                // Invalid JSON or structure - skip this directory
                continue;
            }
        }

        return projects;
    }

    // Private helper methods

    private async Task CheckForNameConflictAsync(string projectDir, string jsonPath, string projectName)
    {
        if (!await _fileStorage.DirectoryExistsAsync(projectDir))
            return;

        if (!await _fileStorage.FileExistsAsync(jsonPath))
            return;

        string existingJson = await _fileStorage.ReadAllTextAsync(jsonPath);
        var existingData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson, JsonOptions);

        if (existingData == null)
            return;

        if (existingData.TryGetValue("name", out var nameElement))
        {
            string? existingName = nameElement.GetString();
            if (existingName != null && existingName != projectName)
            {
                string cleanedName = FileNameHelper.CleanFileName(projectName);
                throw new InvalidOperationException(
                    $"A project with a similar name already exists. " +
                    $"Directory '{cleanedName}' is used by project '{existingName}'. " +
                    $"Cannot save project '{projectName}'."
                );
            }
        }
    }

    private async Task LoadProjectImagesAsync(ProjectBase project, string projectDir, Dictionary<string, JsonElement> projectData)
    {
        // Get all public instance properties of type ImageData (nullable or not)
        var concreteType = project.GetType();
        var imageProperties = concreteType.GetProperties(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance)
            .Where(p => (p.PropertyType == typeof(ImageData) || p.PropertyType == typeof(ImageData?)) && p.CanWrite);

        foreach (var property in imageProperties)
        {
            // Try to get the path from JSON (e.g., "displayImagePath" for "DisplayImage")
            string pathPropertyName = $"{char.ToLowerInvariant(property.Name[0])}{property.Name.Substring(1)}Path";

            string? relativePath = null;
            if (projectData.TryGetValue(pathPropertyName, out var pathElement))
            {
                relativePath = pathElement.GetString();
            }

            if (relativePath == null)
                continue;

            byte[]? imageData = await _imageHelper.LoadImageAsync(relativePath, projectDir);

            if (imageData != null)
            {
                ImageData image = new(imageData);
                property.SetValue(project, image);
            }
        }
    }

    private void DeserializeProjectProperties(ProjectBase project, Dictionary<string, JsonElement> projectData, string projectDir)
    {
        var concreteType = project.GetType();

        // Get all ImageData property names to skip (they're loaded separately via LoadProjectImagesAsync)
        var imagePropertyNames = concreteType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(ImageData) || p.PropertyType == typeof(ImageData?))
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in projectData)
        {
            // Skip system properties
            if (kvp.Key.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("type", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip all image properties (detected generically)
            if (imagePropertyNames.Contains(kvp.Key))
                continue;

            // Skip image path properties (they're used by LoadProjectImagesAsync)
            if (kvp.Key.EndsWith("Path", StringComparison.OrdinalIgnoreCase))
                continue;

            // Handle transformations specially (Option A: object with GUID keys)
            if (kvp.Key.Equals("transformations", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    DeserializeTransformationsFromOptionA(project, kvp.Value);
                }
                catch
                {
                    // Skip if transformations can't be deserialized
                }
                continue;
            }

            // Find matching property (case-insensitive for camelCase JSON)
            var prop = concreteType.GetProperty(kvp.Key, 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.IgnoreCase);

            if (prop != null && prop.CanWrite)
            {
                try
                {
                    object? value = JsonSerializer.Deserialize(kvp.Value.GetRawText(), prop.PropertyType, JsonOptions);
                    prop.SetValue(project, value);
                }
                catch
                {
                    // Skip properties that can't be deserialized
                }
            }
        }
    }

    /// <summary>
    /// Deserializes transformations from Option A structure (object with GUID keys).
    /// Icon is derived from Type via TransformationTypeRegistry, not stored in JSON.
    /// </summary>
    private void DeserializeTransformationsFromOptionA(ProjectBase project, JsonElement transformationsElement)
    {
        project.Transformations.Clear();

        if (transformationsElement.ValueKind != JsonValueKind.Object)
            return;

        foreach (var kvp in transformationsElement.EnumerateObject())
        {
            try
            {
                // Key is the GUID, Value is the transformation object
                if (!Guid.TryParse(kvp.Name, out var transformationId))
                    continue;

                var transformationObj = kvp.Value;
                if (transformationObj.ValueKind != JsonValueKind.Object)
                    continue;

                // Extract type
                if (!transformationObj.TryGetProperty("type", out var typeElement))
                    continue;

                string? typeName = typeElement.GetString();
                if (string.IsNullOrEmpty(typeName))
                    continue;

                // Get icon from registry (not stored in JSON) - can be null if registry not ready
                ImageData? icon = null;
                try
                {
                    icon = TransformationTypeRegistry.GetIcon(typeName);
                }
                catch
                {
                    // Registry might not be initialized yet, continue without icon
                }

                // Create TransformationDTO
                var transformationDto = new TransformationDTO
                {
                    Id = transformationId,
                    Type = typeName,
                    Icon = icon
                };

                project.Transformations.Add(transformationDto);
            }
            catch
            {
                // Skip this transformation if any error occurs
                continue;
            }
        }
    }
}
