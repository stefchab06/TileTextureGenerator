using System.Text.Json;
using System.Text.Json.Serialization;
using TileTextureGenerator.Adapters.Persistence.Ports;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Adapters.Persistence.Stores;

/// <summary>
/// JSON-based implementation of IProjectsStore.
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
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Initializes a new instance of JsonProjectsStore.
    /// </summary>
    /// <param name="fileStorage">File storage abstraction for I/O operations.</param>
    public JsonProjectsStore(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _imageHelper = new ImagePersistenceHelper(fileStorage);

        string appDataPath = _fileStorage.GetApplicationDataPath();
        _projectsBasePath = Path.Combine(appDataPath, "TileTextureGenerator", "Projects");
    }

    /// <inheritdoc />
    public async Task SaveAsync(ProjectBase project)
    {
        ArgumentNullException.ThrowIfNull(project);

        // Clean the project name for directory/file naming
        string cleanedName = FileNameHelper.CleanFileName(project.Name);
        string projectDir = Path.Combine(_projectsBasePath, cleanedName);
        string jsonPath = Path.Combine(projectDir, $"{cleanedName}.json");

        // Check for name conflict
        await CheckForNameConflictAsync(projectDir, jsonPath, project.Name);

        // Create directory structure
        await _fileStorage.EnsureDirectoryExistsAsync(projectDir);
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Sources"));
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Workspace"));
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Outputs"));

        // Save images
        await SaveProjectImagesAsync(project, projectDir);

        // Serialize project to JSON (excluding images, using paths)
        string jsonContent = SerializeProject(project, projectDir);
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

        // Load images
        await LoadProjectImagesAsync(project, projectDir);

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

    private async Task SaveProjectImagesAsync(ProjectBase project, string projectDir)
    {
        // Get all public instance properties of type byte[] (nullable or not)
        var concreteType = project.GetType();
        var imageProperties = concreteType.GetProperties(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(byte[]) && p.CanRead);

        foreach (var property in imageProperties)
        {
            var imageData = property.GetValue(project) as byte[];
            if (imageData != null && imageData.Length > 0)
            {
                await _imageHelper.SavePropertyImageAsync(
                    imageData,
                    projectDir,
                    "Sources",
                    property.Name
                );
            }
        }
    }

    private async Task LoadProjectImagesAsync(ProjectBase project, string projectDir)
    {
        // Get all public instance properties of type byte[] (nullable or not)
        var concreteType = project.GetType();
        var imageProperties = concreteType.GetProperties(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(byte[]) && p.CanWrite);

        foreach (var property in imageProperties)
        {
            string jsonPath = $"Sources/{property.Name}.png";
            byte[]? imageData = await _imageHelper.LoadImageAsync(jsonPath, projectDir);

            if (imageData != null)
            {
                property.SetValue(project, imageData);
            }
        }
    }

    private string SerializeProject(ProjectBase project, string projectDir)
    {
        // Get all byte[] property names for later reference
        var imagePropertyNames = project.GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(byte[]))
            .Select(p => p.Name.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Serialize the entire concrete object
        string json = JsonSerializer.Serialize(project, project.GetType(), JsonOptions);

        // Parse to modify image paths
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            foreach (var property in root.EnumerateObject())
            {
                // Preserve transformations as-is (will be handled by specialized store later)
                // Do NOT skip them - they must be preserved in the JSON

                // Replace all image byte arrays with paths
                if (imagePropertyNames.Contains(property.Name))
                {
                    // Convert property name to PascalCase for filename
                    string propertyName = char.ToUpperInvariant(property.Name[0]) + property.Name.Substring(1);
                    writer.WriteString(property.Name, PathHelper.ToJsonPath($"Sources/{propertyName}.png"));
                }
                else
                {
                    // Write everything else as-is (including transformations)
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private void DeserializeProjectProperties(ProjectBase project, Dictionary<string, JsonElement> projectData, string projectDir)
    {
        var concreteType = project.GetType();

        // Get all byte[] property names to skip (they're loaded separately via LoadProjectImagesAsync)
        var imagePropertyNames = concreteType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(byte[]))
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in projectData)
        {
            // Skip system properties and transformations (handled separately)
            if (kvp.Key.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("type", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("transformations", StringComparison.OrdinalIgnoreCase))  // TODO: Load transformations when TransformationsStore is implemented
                continue;

            // Skip all image properties (detected generically)
            if (imagePropertyNames.Contains(kvp.Key))
                continue;

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
}
