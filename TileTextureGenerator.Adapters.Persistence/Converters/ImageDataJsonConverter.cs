using System.Text.Json;
using System.Text.Json.Serialization;
using TileTextureGenerator.Core.Models;

namespace TileTextureGenerator.Adapters.Persistence.Converters;

/// <summary>
/// JSON converter for ImageData type.
/// Images are not serialized to JSON - they are saved as separate PNG files.
/// This converter ensures ImageData properties are skipped during JSON serialization.
/// </summary>
public class ImageDataJsonConverter : JsonConverter<ImageData>
{
    public override ImageData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Images are loaded from separate PNG files, not from JSON
        // Skip the JSON value (null is expected)
        if (reader.TokenType == JsonTokenType.Null)
        {
            reader.Read();
            return default;
        }

        throw new JsonException("ImageData should not be serialized in JSON. Images are stored as separate PNG files.");
    }

    public override void Write(Utf8JsonWriter writer, ImageData value, JsonSerializerOptions options)
    {
        // Images are saved as separate PNG files, not in JSON
        // Write null to JSON
        writer.WriteNullValue();
    }
}

/// <summary>
/// JSON converter for nullable ImageData type.
/// </summary>
public class NullableImageDataJsonConverter : JsonConverter<ImageData?>
{
    public override ImageData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Images are loaded from separate PNG files, not from JSON
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        throw new JsonException("ImageData should not be serialized in JSON. Images are stored as separate PNG files.");
    }

    public override void Write(Utf8JsonWriter writer, ImageData? value, JsonSerializerOptions options)
    {
        // Images are saved as separate PNG files, not in JSON
        writer.WriteNullValue();
    }
}
