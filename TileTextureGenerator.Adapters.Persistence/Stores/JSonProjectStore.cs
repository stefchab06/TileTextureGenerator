using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TileTextureGenerator.Adapters.Persistence.Converters;
using TileTextureGenerator.Adapters.Persistence.Ports;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;

namespace TileTextureGenerator.Adapters.Persistence.Stores;

internal class JSonProjectStore: IProjectStore
{
    private readonly IFileStorage _fileStorage;
    private readonly ImagePersistenceHelper _imageHelper;
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

    public JSonProjectStore(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
        _imageHelper = new ImagePersistenceHelper(fileStorage);
    }

    async Task IProjectStore.SaveAsync(ProjectBase project)
    {
        ArgumentNullException.ThrowIfNull(project);

        string cleanedName = FileNameHelper.CleanFileName(project.Name);
        string projectDir = _fileStorage.GetProjectPath(cleanedName);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);

        // Ensure directory structure exists
        await _fileStorage.EnsureDirectoryExistsAsync(projectDir);
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Sources"));
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Workspace"));
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Outputs"));

        // Save all image properties and get their paths
        var imagePaths = await SaveProjectImagesAsync(project, projectDir);

        // Serialize the concrete project type polymorphically
        JsonObject jsonDoc;
        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        jsonDoc = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        var properties = project.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in properties)
        {
            if (!prop.CanRead)
                continue;

            if (prop.Name == nameof(project.Transformations))
                continue;

            if (prop.PropertyType == typeof(ImageData) || prop.PropertyType == typeof(ImageData?))
                continue;

            object? value = prop.GetValue(project);

            if (value == null)
            {
                jsonDoc.Remove(prop.Name);
                continue;
            }

            jsonDoc[prop.Name] = JsonSerializer.SerializeToNode(value);
        }

        // add image path properties
        foreach (var kvp in imagePaths)
        {
            // Add path property: e.g., "displayImagePath": "Sources/DisplayImage.png"
            string pathPropertyName = $"{char.ToLowerInvariant(kvp.Key[0])}{kvp.Key.Substring(1)}Path";
            jsonDoc[pathPropertyName] = JsonSerializer.SerializeToNode(kvp.Value, JsonOptions);
        }

        string updatedJson = jsonDoc.ToJsonString(JsonOptions);
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);
    }

    async Task IProjectStore.AddTransformationAsync(ProjectBase project, TransformationDTO transformation)
    {
        string cleanedName = FileNameHelper.CleanFileName(project.Name);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);
        JsonObject jsonDoc;
        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        jsonDoc = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        // TODO: add a node in Transformations node: { Node name: transformation.Id.ToString, value: transformation }

        json = jsonDoc.ToJsonString(JsonOptions);
        await _fileStorage.WriteAllTextAsync(jsonPath, json);
    }
    async Task IProjectStore.RemoveTransformationAsync(ProjectBase project, Guid transformationID)
    {
        string cleanedName = FileNameHelper.CleanFileName(project.Name);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);
        JsonObject jsonDoc;
        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        jsonDoc = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        // Delete node with name transformationID.ToString() from Transformations node

        json = jsonDoc.ToJsonString(JsonOptions);
        await _fileStorage.WriteAllTextAsync(jsonPath, json);
    }

    // Private helper methods

    private async Task<Dictionary<string, string>> SaveProjectImagesAsync(ProjectBase project, string projectDir)
    {
        var imagePaths = new Dictionary<string, string>();

        // Get all public instance properties of type ImageData (nullable or not)
        var concreteType = project.GetType();
        var imageProperties = concreteType.GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance)
            .Where(p => (p.PropertyType == typeof(ImageData) || p.PropertyType == typeof(ImageData?)) && p.CanRead);

        foreach (var property in imageProperties)
        {
            object? value = property.GetValue(project);

            if (value is ImageData imageData)
            {
                // Use property name as filename for unique images
                string relativePath = $"Sources/{property.Name}.png";

                await _imageHelper.SavePropertyImageAsync(
                    imageData.Bytes,
                    projectDir,
                    "Sources",
                    property.Name
                );

                imagePaths[property.Name] = relativePath;
            }
        }

        return imagePaths;
    }
}
