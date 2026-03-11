using System.Text.Json;
using System.Text.Json.Serialization;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Transformations;

namespace TileTextureGenerator.Core.Serialization;

/// <summary>
/// Custom JSON converter for TransformationEntity.
/// Handles serialization/deserialization of transformation configurations including
/// dynamic properties and edge flap configurations.
/// </summary>
public class TransformationEntityJsonConverter : JsonConverter<TransformationEntity>
{
    public override TransformationEntity Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var entity = new TransformationEntity
        {
            Id = Guid.Parse(root.GetProperty("Id").GetString()!),
            TransformationType = root.GetProperty("TransformationType").GetString()!,
            DisplayOrder = root.GetProperty("DisplayOrder").GetInt32(),
            IsGenerated = root.GetProperty("IsGenerated").GetBoolean(),
            CreatedDate = root.GetProperty("CreatedDate").GetDateTime(),
            ModifiedDate = root.GetProperty("ModifiedDate").GetDateTime()
        };

        // Optional properties
        if (root.TryGetProperty("LastGeneratedDate", out var genDate) && genDate.ValueKind != JsonValueKind.Null)
        {
            entity.LastGeneratedDate = genDate.GetDateTime();
        }

        // Deserialize Properties dictionary
        if (root.TryGetProperty("Properties", out var propsElement))
        {
            entity.Properties = DeserializeProperties(propsElement);
        }

        return entity;
    }

    public override void Write(
        Utf8JsonWriter writer,
        TransformationEntity value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Id", value.Id);
        writer.WriteString("TransformationType", value.TransformationType);
        writer.WriteNumber("DisplayOrder", value.DisplayOrder);
        writer.WriteBoolean("IsGenerated", value.IsGenerated);
        writer.WriteString("CreatedDate", value.CreatedDate);
        writer.WriteString("ModifiedDate", value.ModifiedDate);

        // Optional LastGeneratedDate
        if (value.LastGeneratedDate.HasValue)
        {
            writer.WriteString("LastGeneratedDate", value.LastGeneratedDate.Value);
        }
        else
        {
            writer.WriteNull("LastGeneratedDate");
        }

        // Serialize Properties dictionary
        writer.WritePropertyName("Properties");
        SerializeProperties(writer, value.Properties, options);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Serializes the Properties dictionary, handling special types like EdgeFlapsCollection.
    /// </summary>
    private void SerializeProperties(
        Utf8JsonWriter writer,
        Dictionary<string, object> properties,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in properties)
        {
            writer.WritePropertyName(kvp.Key);

            // Special handling for EdgeFlapsCollection
            if (kvp.Value is EdgeFlapsCollection edgeFlaps)
            {
                SerializeEdgeFlaps(writer, edgeFlaps, options);
            }
            // Special handling for enums
            else if (kvp.Value is Enum enumValue)
            {
                writer.WriteStringValue(enumValue.ToString());
            }
            // Default serialization for primitives and other types
            else
            {
                JsonSerializer.Serialize(writer, kvp.Value, kvp.Value.GetType(), options);
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Deserializes the Properties dictionary, reconstructing special types.
    /// </summary>
    private Dictionary<string, object> DeserializeProperties(JsonElement propsElement)
    {
        var properties = new Dictionary<string, object>();

        foreach (var prop in propsElement.EnumerateObject())
        {
            // Check if it's EdgeFlapsCollection (has North, South, East, West properties)
            if (prop.Value.ValueKind == JsonValueKind.Object &&
                prop.Value.TryGetProperty("North", out _))
            {
                properties[prop.Name] = DeserializeEdgeFlaps(prop.Value);
            }
            // Handle different value types
            else
            {
                properties[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.TryGetInt32(out var i) ? i : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null!,
                    _ => prop.Value.GetRawText()
                };
            }
        }

        return properties;
    }

    /// <summary>
    /// Serializes an EdgeFlapsCollection.
    /// </summary>
    private void SerializeEdgeFlaps(
        Utf8JsonWriter writer,
        EdgeFlapsCollection edgeFlaps,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        SerializeEdgeFlapConfig(writer, "North", edgeFlaps.North, options);
        SerializeEdgeFlapConfig(writer, "South", edgeFlaps.South, options);
        SerializeEdgeFlapConfig(writer, "East", edgeFlaps.East, options);
        SerializeEdgeFlapConfig(writer, "West", edgeFlaps.West, options);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Serializes a single EdgeFlapConfiguration.
    /// Note: TextureImage (byte[]) is not serialized - managed separately by persistence layer.
    /// </summary>
    private void SerializeEdgeFlapConfig(
        Utf8JsonWriter writer,
        string propertyName,
        EdgeFlapConfiguration config,
        JsonSerializerOptions options)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartObject();

        writer.WriteString("Mode", config.Mode.ToString());

        if (config.Color != null)
        {
            writer.WriteString("Color", config.Color);
        }
        else
        {
            writer.WriteNull("Color");
        }

        // TextureImage (byte[]) is NOT serialized here
        // It's managed by the persistence layer separately

        writer.WriteEndObject();
    }

    /// <summary>
    /// Deserializes an EdgeFlapsCollection.
    /// </summary>
    private EdgeFlapsCollection DeserializeEdgeFlaps(JsonElement element)
    {
        var edgeFlaps = new EdgeFlapsCollection();

        if (element.TryGetProperty("North", out var north))
            edgeFlaps.North = DeserializeEdgeFlapConfig(north);

        if (element.TryGetProperty("South", out var south))
            edgeFlaps.South = DeserializeEdgeFlapConfig(south);

        if (element.TryGetProperty("East", out var east))
            edgeFlaps.East = DeserializeEdgeFlapConfig(east);

        if (element.TryGetProperty("West", out var west))
            edgeFlaps.West = DeserializeEdgeFlapConfig(west);

        return edgeFlaps;
    }

    /// <summary>
    /// Deserializes a single EdgeFlapConfiguration.
    /// Note: TextureImage will be loaded separately by the persistence layer.
    /// </summary>
    private EdgeFlapConfiguration DeserializeEdgeFlapConfig(JsonElement element)
    {
        var config = new EdgeFlapConfiguration();

        if (element.TryGetProperty("Mode", out var mode))
        {
            config.Mode = Enum.Parse<EdgeFlapMode>(mode.GetString()!);
        }

        if (element.TryGetProperty("Color", out var color) && color.ValueKind != JsonValueKind.Null)
        {
            config.Color = color.GetString();
        }

        // TextureImage will be loaded by the persistence layer

        return config;
    }
}
