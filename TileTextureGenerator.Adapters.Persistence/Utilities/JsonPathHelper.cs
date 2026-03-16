using System.Text.Json;

namespace TileTextureGenerator.Adapters.Persistence.Utilities;

/// <summary>
/// Helper class for setting and getting values at complex JSON paths.
/// Supports paths like "Transformations.{guid}.EdgeFlap[Top].Texture".
/// </summary>
public static class JsonPathHelper
{
    /// <summary>
    /// Sets a value at a specific path in a JSON document.
    /// Path format: "property.subproperty[key].nested"
    /// Examples: 
    /// - "displayImage" → root property
    /// - "transformations.abc-123.icon" → nested object
    /// - "transformations.abc-123.edgeFlaps[Top].texture" → nested with indexer
    /// </summary>
    /// <param name="jsonDocument">The JSON document as a string.</param>
    /// <param name="path">The dot-separated path to the property.</param>
    /// <param name="value">The value to set (will be JSON serialized).</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>Modified JSON document as string.</returns>
    public static string SetValueAtPath(
        string jsonDocument,
        string path,
        object? value,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));

        // Parse JSON
        using var doc = JsonDocument.Parse(jsonDocument);
        var root = doc.RootElement;

        // Build new JSON with modification
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        WriteModifiedJson(writer, root, path.Split('.'), 0, value, options);

        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Gets a value at a specific path in a JSON document.
    /// </summary>
    /// <param name="jsonDocument">The JSON document as a string.</param>
    /// <param name="path">The dot-separated path to the property.</param>
    /// <returns>The value at the path as a string, or null if not found.</returns>
    public static string? GetValueAtPath(string jsonDocument, string path)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));

        using var doc = JsonDocument.Parse(jsonDocument);
        var current = doc.RootElement;
        var parts = path.Split('.');

        foreach (var part in parts)
        {
            // Handle array indexing like "edgeFlaps[Top]"
            if (part.Contains('[') && part.Contains(']'))
            {
                string propertyName = part.Substring(0, part.IndexOf('['));
                string key = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);

                if (!current.TryGetProperty(propertyName, out var arrayOrObject))
                    return null;

                // Try as object with key
                if (arrayOrObject.ValueKind == JsonValueKind.Object)
                {
                    if (!arrayOrObject.TryGetProperty(key, out current))
                        return null;
                }
                else
                {
                    return null; // Array indexing not implemented yet
                }
            }
            else
            {
                // Regular property access
                if (!current.TryGetProperty(part, out current))
                    return null;
            }
        }

        // Return as string (copy the value before disposing the document)
        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => current.GetRawText() // For objects/arrays, return raw JSON
        };
    }

    // Private helper methods

    private static void WriteModifiedJson(
        Utf8JsonWriter writer,
        JsonElement element,
        string[] pathParts,
        int currentDepth,
        object? newValue,
        JsonSerializerOptions? options)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            // Not an object, write as-is
            element.WriteTo(writer);
            return;
        }

        writer.WriteStartObject();

        string currentPart = pathParts[currentDepth];
        bool isLastPart = currentDepth == pathParts.Length - 1;

        foreach (var property in element.EnumerateObject())
        {
            // Check if this property matches the current path part
            bool isTargetProperty = property.Name.Equals(currentPart, StringComparison.OrdinalIgnoreCase);

            if (isTargetProperty && isLastPart)
            {
                // This is the property to modify
                writer.WritePropertyName(property.Name);
                if (newValue == null)
                {
                    writer.WriteNullValue();
                }
                else if (newValue is string str)
                {
                    writer.WriteStringValue(str);
                }
                else
                {
                    // Serialize complex objects
                    string json = JsonSerializer.Serialize(newValue, options);
                    using var tempDoc = JsonDocument.Parse(json);
                    tempDoc.RootElement.WriteTo(writer);
                }
            }
            else if (isTargetProperty && !isLastPart)
            {
                // This property is part of the path, recurse into it
                writer.WritePropertyName(property.Name);
                WriteModifiedJson(writer, property.Value, pathParts, currentDepth + 1, newValue, options);
            }
            else
            {
                // Not the target property, copy as-is
                property.WriteTo(writer);
            }
        }

        // If the property doesn't exist yet and we're at the right level, create it
        if (isLastPart && !element.TryGetProperty(currentPart, out _))
        {
            writer.WritePropertyName(currentPart);
            if (newValue == null)
            {
                writer.WriteNullValue();
            }
            else if (newValue is string str)
            {
                writer.WriteStringValue(str);
            }
            else
            {
                string json = JsonSerializer.Serialize(newValue, options);
                using var tempDoc = JsonDocument.Parse(json);
                tempDoc.RootElement.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }
}
