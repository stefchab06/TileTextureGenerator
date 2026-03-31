using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TileTextureGenerator.Adapters.Persistence.Converters;
using TileTextureGenerator.Adapters.Persistence.Ports;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;

namespace TileTextureGenerator.Adapters.Persistence.Stores;

/// <summary>
/// JSON-based implementation of ITransformationStore.
/// Handles persistence of transformation entities to file system.
/// </summary>
internal class JSonTransformationStore : ITransformationStore
{
    private readonly IFileStorage _fileStorage;
    private readonly ImagePersistenceHelper _imageHelper;
    private readonly ProjectJsonHelper _jsonHelper;
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

    public JSonTransformationStore(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
        _imageHelper = new ImagePersistenceHelper(fileStorage);
        _jsonHelper = new ProjectJsonHelper(fileStorage);
    }

    /// <inheritdoc />
    async Task ITransformationStore.SaveAsync(TransformationBase transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);

        // Get project directory path
        string cleanedName = FileNameHelper.CleanFileName(transformation.ParentProject.Name);
        string projectDir = _fileStorage.GetProjectPath(cleanedName);

        // Ensure Workspace and Outputs directories exist
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Workspace"));
        await _fileStorage.EnsureDirectoryExistsAsync(Path.Combine(projectDir, "Outputs"));

        // Load existing JSON using helper
        JsonObject jsonDoc = await _jsonHelper.LoadProjectJsonAsync(transformation.ParentProject.Name);

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

        // Save updated JSON using helper
        await _jsonHelper.SaveProjectJsonAsync(transformation.ParentProject.Name, jsonDoc, JsonOptions);
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

            // Skip at root level: ParentProject, Id, Type (Type already handled separately)
            // Note: Icon property was removed from TransformationBase (replaced by static IconResourceName)
            if (prop.Name == nameof(transformation.ParentProject) || 
                prop.Name == nameof(transformation.Id) || 
                prop.Name == nameof(transformation.Type))
                continue;

            var value = prop.GetValue(transformation);
            if (value == null) continue; // Don't save null values

            // Special handling for ImageData at root level
            if (prop.PropertyType == typeof(ImageData) || prop.PropertyType == typeof(ImageData?))
            {
                var imageData = (ImageData)value;

                // Special handling for GeneratedTexture - save in Outputs folder
                if (prop.Name == nameof(transformation.GeneratedTexture))
                {
                    var (jsonPropertyName, jsonPathValue) = await _imageHelper.SerializeTransformationOutputImageDataAsync(
                        prop.Name,
                        imageData,
                        projectDir,
                        existingNode
                    );

                    transformationNode[jsonPropertyName] = jsonPathValue;
                }
                else
                {
                    // Use helper to serialize transformation ImageData in Workspace (with GUID reuse)
                    var (jsonPropertyName, jsonPathValue) = await _imageHelper.SerializeTransformationImageDataAsync(
                        prop.Name,
                        imageData,
                        projectDir,
                        existingNode
                    );

                    // Add path property directly to transformation node
                    transformationNode[jsonPropertyName] = jsonPathValue;
                }
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

                    // Use helper to serialize transformation ImageData (with GUID reuse)
                    var (jsonPropertyName, jsonPathValue) = await _imageHelper.SerializeTransformationImageDataAsync(
                        prop.Name,
                        imageData,
                        projectDir,
                        existingNode
                    );

                    // Add path property to the nested object
                    jsonObject[jsonPropertyName] = jsonPathValue;
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
}
