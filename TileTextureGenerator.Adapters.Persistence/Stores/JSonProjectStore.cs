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
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Adapters.Persistence.Stores;

internal class JSonProjectStore: IProjectStore, ITransformationStore
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

        // Check if file exists before reading
        if (await _fileStorage.FileExistsAsync(jsonPath))
        {
            string json = await _fileStorage.ReadAllTextAsync(jsonPath);
            jsonDoc = string.IsNullOrWhiteSpace(json)
                ? new JsonObject()
                : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }
        else
        {
            jsonDoc = new JsonObject();
        }

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

            // Convert property name to camelCase for JSON key
            string jsonKey = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

            if (value == null)
            {
                jsonDoc.Remove(jsonKey);
                continue;
            }

            jsonDoc[jsonKey] = JsonSerializer.SerializeToNode(value, JsonOptions);
        }

        // add image path properties
        foreach (var kvp in imagePaths)
        {
            // Add path property: e.g., "displayimagePath": "Sources/DisplayImage.png" (fully lowercase + Path)
            string pathPropertyName = $"{kvp.Key.ToLowerInvariant()}Path";
            jsonDoc[pathPropertyName] = JsonSerializer.SerializeToNode(kvp.Value, JsonOptions);
        }

        string updatedJson = jsonDoc.ToJsonString(JsonOptions);
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);
    }

    /// <inheritdoc />
    async Task IProjectStore.AddTransformationAsync(ProjectBase project, TransformationDTO transformation)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(transformation);

        string cleanedName = FileNameHelper.CleanFileName(project.Name);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);

        // Load existing JSON
        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        JsonObject jsonDoc = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        // Get or create "transformations" node as JsonObject
        JsonObject transformationsNode;
        if (jsonDoc.TryGetPropertyValue("transformations", out var existingNode) && 
            existingNode is JsonObject existingObj)
        {
            transformationsNode = existingObj;
        }
        else
        {
            transformationsNode = new JsonObject();
            jsonDoc["transformations"] = transformationsNode;
        }

        // Add transformation with GUID as key, excluding Icon (derived from Type via registry)
        var transformationObj = new JsonObject
        {
            ["type"] = transformation.Type
            // Icon is intentionally excluded - it's derived from Type via TransformationTypeRegistry
        };

        transformationsNode[transformation.Id.ToString()] = transformationObj;

        // Save updated JSON
        string updatedJson = jsonDoc.ToJsonString(JsonOptions);
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);
    }

    /// <inheritdoc />
    async Task IProjectStore.RemoveTransformationAsync(ProjectBase project, Guid transformationID)
    {
        ArgumentNullException.ThrowIfNull(project);

        string cleanedName = FileNameHelper.CleanFileName(project.Name);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);

        // Load existing JSON
        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        JsonObject jsonDoc = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        // Find "transformations" node
        if (jsonDoc.TryGetPropertyValue("transformations", out var transformationsNode) &&
            transformationsNode is JsonObject transformationsObj)
        {
            // Remove the transformation by GUID key
            string transformationKey = transformationID.ToString();
            transformationsObj.Remove(transformationKey);

            // If transformations object is now empty, remove the entire "transformations" node
            if (transformationsObj.Count == 0)
            {
                jsonDoc.Remove("transformations");
            }
        }

        // Save updated JSON
        string updatedJson = jsonDoc.ToJsonString(JsonOptions);
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);
    }

    /// <inheritdoc />
    async Task<TransformationBase> IProjectStore.LoadTransformationAsync(ProjectBase project, Guid transformationID)
    {
        ArgumentNullException.ThrowIfNull(project);

        string cleanedName = FileNameHelper.CleanFileName(project.Name);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);

        // Load existing JSON
        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        JsonObject jsonDoc = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        // Find "transformations" node
        if (!jsonDoc.TryGetPropertyValue("transformations", out var transformationsNode) ||
            transformationsNode is not JsonObject transformationsObj)
        {
            throw new InvalidOperationException($"No transformations found in project '{project.Name}'.");
        }

        // Find the specific transformation by GUID key
        string transformationKey = transformationID.ToString();
        if (!transformationsObj.TryGetPropertyValue(transformationKey, out var transformationNode) ||
            transformationNode is not JsonObject transformationObj)
        {
            throw new InvalidOperationException($"Transformation with ID '{transformationID}' not found in project '{project.Name}'.");
        }

        // Extract type
        if (!transformationObj.TryGetPropertyValue("type", out var typeNode))
        {
            throw new InvalidOperationException($"Transformation '{transformationID}' is missing 'type' property.");
        }

        string? typeName = typeNode?.GetValue<string>();
        if (string.IsNullOrEmpty(typeName))
        {
            throw new InvalidOperationException($"Transformation '{transformationID}' has invalid 'type' value.");
        }

        // Get the type from registry
        Type? transformationType = TransformationTypeRegistry.GetTypeByName(typeName);
        if (transformationType == null)
        {
            throw new InvalidOperationException($"Unknown transformation type: '{typeName}'. Make sure it's registered.");
        }

        // Create instance using factory (without calling Initialize yet)
        TransformationBase transformation;
        try
        {
            // Access internal factory through reflection to avoid double initialization
            var factoryField = typeof(TransformationTypeRegistry).GetField("_factory", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (factoryField?.GetValue(null) is not Func<Type, TransformationBase> factory)
            {
                throw new InvalidOperationException("Factory not set. Call SetFactory before loading transformations.");
            }

            transformation = factory(transformationType);
            transformation.Initialize(project, transformationID); // Use the ID from JSON
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to create transformation of type '{typeName}'.", ex);
        }

        // Deserialize all properties from JSON
        await DeserializeTransformationPropertiesAsync(transformation, transformationObj, project);

        return transformation;
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

    /// <summary>
    /// Deserializes all properties of a transformation from JSON using generic approach.
    /// Handles ImageData loading recursively at any nesting level.
    /// </summary>
    private async Task DeserializeTransformationPropertiesAsync(TransformationBase transformation, JsonObject jsonObj, ProjectBase project)
    {
        var concreteType = transformation.GetType();
        string projectDir = _fileStorage.GetProjectPath(FileNameHelper.CleanFileName(project.Name));

        // Step 1: Standard JSON deserialization (ignoring "type" and "xxxPath" properties)
        var filteredJson = new JsonObject();
        foreach (var kvp in jsonObj)
        {
            // Skip system properties
            if (kvp.Key.Equals("type", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip path properties (they'll be handled in Step 2)
            if (kvp.Key.EndsWith("Path", StringComparison.OrdinalIgnoreCase))
                continue;

            filteredJson[kvp.Key] = kvp.Value?.DeepClone();
        }

        // Skip JsonSerializer.Deserialize due to constructor issues, use property-by-property directly
        await DeserializePropertiesIndividually(transformation, jsonObj);

        // Step 2: Recursive ImageData loading from xxxPath properties
        await LoadImageDataRecursivelyAsync(transformation, jsonObj, projectDir);
    }

    /// <summary>
    /// Copies properties from deserialized temp object to actual transformation (excluding immutable properties).
    /// </summary>
    private void CopyDeserializedProperties(object source, TransformationBase target)
    {
        var sourceType = source.GetType();
        var targetType = target.GetType();

        var properties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Skip immutable properties (Id, ParentProject, etc.)
            if (prop.Name == nameof(target.Id) || 
                prop.Name == nameof(target.ParentProject) || 
                prop.Name == nameof(target.Type) ||
                !prop.CanWrite)
            {
                continue;
            }

            try
            {
                var value = prop.GetValue(source);
                var targetProp = targetType.GetProperty(prop.Name);
                if (targetProp != null && targetProp.CanWrite)
                {
                    targetProp.SetValue(target, value);
                }
            }
            catch
            {
                // Skip properties that can't be copied
            }
        }
    }

    /// <summary>
    /// Fallback: deserialize properties individually if bulk deserialization fails.
    /// </summary>
    private async Task DeserializePropertiesIndividually(TransformationBase transformation, JsonObject jsonObj)
    {
        var concreteType = transformation.GetType();

        foreach (var kvp in jsonObj)
        {
            // Skip system properties and paths
            if (kvp.Key.Equals("type", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.EndsWith("Path", StringComparison.OrdinalIgnoreCase))
                continue;

            // Find matching property (case-insensitive, include inherited properties)
            var prop = concreteType.GetProperty(kvp.Key, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);

            if (prop != null && prop.CanWrite)
            {
                // Skip immutable properties
                if (prop.Name == nameof(transformation.Id) || 
                    prop.Name == nameof(transformation.ParentProject) || 
                    prop.Name == nameof(transformation.Type))
                    continue;

                try
                {
                    object? value = JsonSerializer.Deserialize(kvp.Value.ToJsonString(), prop.PropertyType, JsonOptions);
                    prop.SetValue(transformation, value);
                }
                catch
                {
                    // Skip properties that can't be deserialized
                }
            }
        }
    }

    /// <summary>
    /// Recursively searches for ImageData properties and loads them from corresponding xxxPath entries.
    /// </summary>
    private async Task LoadImageDataRecursivelyAsync(object obj, JsonObject jsonObj, string projectDir)
    {
        if (obj == null) return;

        var objType = obj.GetType();
        var properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanRead) continue;

            try
            {
                var value = prop.GetValue(obj);

                // Handle ImageData and ImageData? properties
                if (prop.PropertyType == typeof(ImageData) || prop.PropertyType == typeof(ImageData?))
                {
                    await LoadImageDataPropertyAsync(obj, prop, jsonObj, projectDir);
                }
                // Handle collections (arrays, lists)
                else if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    foreach (var item in enumerable)
                    {
                        if (item != null && !IsSimpleType(item.GetType()))
                        {
                            await LoadImageDataRecursivelyAsync(item, jsonObj, projectDir);
                        }
                    }
                }
                // Handle complex objects (not simple types)
                else if (value != null && !IsSimpleType(prop.PropertyType))
                {
                    await LoadImageDataRecursivelyAsync(value, jsonObj, projectDir);
                }
            }
            catch
            {
                // Skip properties that can't be processed
            }
        }
    }

    /// <summary>
    /// Loads ImageData for a specific property from corresponding xxxPath in JSON.
    /// </summary>
    private async Task LoadImageDataPropertyAsync(object obj, PropertyInfo imageProperty, JsonObject jsonObj, string projectDir)
    {
        if (!imageProperty.CanWrite) return;

        // Generate path property name: "BaseTexture" -> "basetexturePath" (fully lowercase + Path)
        string pathPropertyName = $"{imageProperty.Name.ToLowerInvariant()}Path";

        if (jsonObj.TryGetPropertyValue(pathPropertyName, out var pathNode))
        {
            string? imagePath = pathNode?.GetValue<string>();
            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    byte[]? imageBytes = await _imageHelper.LoadImageAsync(imagePath, projectDir);
                    if (imageBytes != null)
                    {
                        ImageData imageData = new ImageData(imageBytes);

                        // Set the ImageData property
                        if (imageProperty.PropertyType == typeof(ImageData?))
                        {
                            imageProperty.SetValue(obj, (ImageData?)imageData);
                        }
                        else if (imageProperty.PropertyType == typeof(ImageData))
                        {
                            imageProperty.SetValue(obj, imageData);
                        }
                    }
                }
                catch
                {
                    // Skip images that can't be loaded
                }
            }
        }
    }

    /// <summary>
    /// Determines if a type is a simple type (primitive, string, enum, etc.) that doesn't need recursive processing.
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || 
               type.IsEnum || 
               type == typeof(string) || 
               type == typeof(DateTime) || 
               type == typeof(DateTimeOffset) || 
               type == typeof(TimeSpan) || 
               type == typeof(Guid) || 
               type == typeof(decimal) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                IsSimpleType(Nullable.GetUnderlyingType(type)!));
    }

    /// <inheritdoc />
    async Task ITransformationStore.SaveAsync(TransformationBase transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);

        // Get project JSON file path via ParentProject
        string cleanedName = FileNameHelper.CleanFileName(transformation.ParentProject.Name);
        string projectDir = _fileStorage.GetProjectPath(cleanedName);
        string jsonPath = _fileStorage.GetProjectFileName(cleanedName);

        // Ensure Workspace directory exists
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Workspace"));

        // Load existing JSON
        string json = await _fileStorage.ReadAllTextAsync(jsonPath);
        JsonObject jsonDoc = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

        // Get or create "transformations" node
        JsonObject transformationsNode;
        if (jsonDoc.TryGetPropertyValue("transformations", out var existingNode) && 
            existingNode is JsonObject existingObj)
        {
            transformationsNode = existingObj;
        }
        else
        {
            transformationsNode = new JsonObject();
            jsonDoc["transformations"] = transformationsNode;
        }

        // Get or create transformation-specific node
        string transformationKey = transformation.Id.ToString();
        JsonObject transformationNode;
        if (transformationsNode.TryGetPropertyValue(transformationKey, out var existingTransformationNode) && 
            existingTransformationNode is JsonObject existingTransformationObj)
        {
            transformationNode = existingTransformationObj;
        }
        else
        {
            transformationNode = new JsonObject();
            transformationsNode[transformationKey] = transformationNode;
        }

        // Save Type (always)
        transformationNode["type"] = transformation.Type;

        // Serialize all transformation properties recursively (handling ImageData at all levels)
        await SerializeTransformationPropertiesAsync(transformation, transformationNode, transformationNode, projectDir);

        // Save updated JSON
        string updatedJson = jsonDoc.ToJsonString(JsonOptions);
        await _fileStorage.WriteAllTextAsync(jsonPath, updatedJson);
    }

    /// <summary>
    /// Serializes all properties of a transformation, handling ImageData specially at all nesting levels.
    /// </summary>
    private async Task SerializeTransformationPropertiesAsync(TransformationBase transformation, JsonObject transformationNode, JsonObject existingNode, string projectDir)
    {
        var properties = transformation.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanRead) continue;

            // Skip indexed properties (like this[ImageSide])
            if (prop.GetIndexParameters().Length > 0) continue;

            // Skip at root level: Icon, ParentProject, Id, Type (Type already handled separately)
            if (prop.Name == nameof(transformation.Icon) || 
                prop.Name == nameof(transformation.ParentProject) || 
                prop.Name == nameof(transformation.Id) || 
                prop.Name == nameof(transformation.Type))
                continue;

            var value = prop.GetValue(transformation);
            if (value == null) continue; // Don't save null values

            // Special handling for ImageData at root level
            if (prop.PropertyType == typeof(ImageData) || prop.PropertyType == typeof(ImageData?))
            {
                var imageData = (ImageData)value;

                // 1. Generate path property name (fully lowercase + Path)
                string pathPropertyName = $"{prop.Name.ToLowerInvariant()}Path";

                // 2. Get existing image file name or generate new GUID
                string imageFileName;
                if (existingNode.TryGetPropertyValue(pathPropertyName, out var existingPathNode) && 
                    existingPathNode is JsonValue jsonValue)
                {
                    try
                    {
                        imageFileName = jsonValue.GetValue<string>(); // Reuse existing GUID
                    }
                    catch
                    {
                        imageFileName = $"Workspace/{Guid.NewGuid()}.png"; // Generate new if parse fails
                    }
                }
                else
                {
                    imageFileName = $"Workspace/{Guid.NewGuid()}.png"; // Generate new GUID
                }

                // 3. Save the image
                string fullImagePath = Path.Combine(projectDir, imageFileName);
                await _fileStorage.WriteAllBytesAsync(fullImagePath, imageData.Bytes);

                // 4. Add path property directly to transformation node
                transformationNode[pathPropertyName] = imageFileName;
            }
            else
            {
                string jsonKey = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

                // Recursively serialize the value
                var serialized = await SerializeValueRecursivelyAsync(value, prop.Name, existingNode, projectDir);

                if (serialized != null)
                    transformationNode[jsonKey] = serialized;
            }
        }
    }

    /// <summary>
    /// Recursively serializes a value, handling ImageData, collections, complex objects, and simple types.
    /// </summary>
    private async Task<JsonNode?> SerializeValueRecursivelyAsync(object? value, string propertyName, JsonObject existingNode, string projectDir)
    {
        if (value == null) return null;

        var valueType = value.GetType();

        // Handle collections (arrays, lists, etc.)
        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var jsonArray = new JsonArray();
            int index = 0;
            foreach (var item in enumerable)
            {
                if (item == null) continue;

                var serializedItem = await SerializeValueRecursivelyAsync(item, $"{propertyName}[{index}]", existingNode, projectDir);
                if (serializedItem != null)
                    jsonArray.Add(serializedItem);

                index++;
            }
            return jsonArray;
        }
        // Handle complex objects
        else if (!IsSimpleType(valueType))
        {
            var jsonObject = new JsonObject();
            var objectProperties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in objectProperties)
            {
                if (!prop.CanRead) continue;

                // Skip indexed properties (like EdgeFlap[ImageSide.Top])
                if (prop.GetIndexParameters().Length > 0) continue;

                var propValue = prop.GetValue(value);
                if (propValue == null) continue;

                // Special handling for ImageData properties in nested objects
                if (prop.PropertyType == typeof(ImageData) || prop.PropertyType == typeof(ImageData?))
                {
                    var imageData = (ImageData)propValue;

                    // 1. Generate path property name (fully lowercase + Path)
                    string pathPropertyName = $"{prop.Name.ToLowerInvariant()}Path";

                    // 2. Get existing image file name or generate new GUID
                    string imageFileName;
                    if (existingNode.TryGetPropertyValue(pathPropertyName, out var existingPathNode) && 
                        existingPathNode is JsonValue jsonValue)
                    {
                        try
                        {
                            imageFileName = jsonValue.GetValue<string>(); // Reuse existing GUID
                        }
                        catch
                        {
                            imageFileName = $"Workspace/{Guid.NewGuid()}.png"; // Generate new if parse fails
                        }
                    }
                    else
                    {
                        imageFileName = $"Workspace/{Guid.NewGuid()}.png"; // Generate new GUID
                    }

                    // 3. Save the image
                    string fullImagePath = Path.Combine(projectDir, imageFileName);
                    await _fileStorage.WriteAllBytesAsync(fullImagePath, imageData.Bytes);

                    // 4. Add path property to the nested object
                    jsonObject[pathPropertyName] = imageFileName;
                }
                else
                {
                    var serialized = await SerializeValueRecursivelyAsync(propValue, prop.Name, existingNode, projectDir);

                    if (serialized != null)
                    {
                        string key = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                        jsonObject[key] = serialized;
                    }
                }
            }
            return jsonObject;
        }
        // Handle simple types (primitives, strings, enums, etc.)
        else
        {
            return JsonSerializer.SerializeToNode(value, JsonOptions);
        }
    }
}
