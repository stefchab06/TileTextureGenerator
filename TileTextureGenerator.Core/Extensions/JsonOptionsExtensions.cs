using System.Text.Json;
using System.Text.Json.Serialization;
using TileTextureGenerator.Core.Serialization;

namespace TileTextureGenerator.Core.Extensions;

/// <summary>
/// Provides configured JsonSerializerOptions for the application.
/// Includes custom converters for transformation entities and other domain types.
/// </summary>
public static class JsonOptionsExtensions
{
    private static JsonSerializerOptions? _defaultOptions;

    /// <summary>
    /// Gets the default JsonSerializerOptions configured with all custom converters.
    /// Options are cached for performance.
    /// </summary>
    public static JsonSerializerOptions GetDefaultOptions()
    {
        if (_defaultOptions == null)
        {
            _defaultOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                Converters =
                {
                    new TransformationEntityJsonConverter(),
                    new JsonStringEnumConverter() // Serialize enums as strings
                }
            };
        }

        return _defaultOptions;
    }

    /// <summary>
    /// Creates a new JsonSerializerOptions with custom converters added.
    /// Use this when you need to modify options without affecting the cached default.
    /// </summary>
    public static JsonSerializerOptions CreateWithConverters()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new TransformationEntityJsonConverter(),
                new JsonStringEnumConverter()
            }
        };
    }

    /// <summary>
    /// Adds transformation converters to existing JsonSerializerOptions.
    /// </summary>
    public static JsonSerializerOptions AddTransformationConverters(this JsonSerializerOptions options)
    {
        options.Converters.Add(new TransformationEntityJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
