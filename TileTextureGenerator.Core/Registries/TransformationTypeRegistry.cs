using System.Reflection;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;

namespace TileTextureGenerator.Core.Registries;

/// <summary>
/// Registry for transformation types.
/// Manages registration, instantiation, and metadata for all available transformation types.
/// </summary>
public static class TransformationTypeRegistry
{
    private static readonly Dictionary<string, Type> _types = new();
    private static Func<Type, Entities.TransformationBase>? _factory;
    private static bool _isInitialized = false;

    /// <summary>
    /// Sets the factory function for creating transformation instances.
    /// Must be called before creating transformations.
    /// </summary>
    /// <param name="factory">Factory function that creates transformation instances from their type.</param>
    public static void SetFactory(Func<Type, Entities.TransformationBase> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Registers a transformation type.
    /// </summary>
    /// <typeparam name="T">The transformation type to register</typeparam>
    public static void Register<T>() where T : TransformationBase
    {
        var typeName = typeof(T).Name;

        if (_types.ContainsKey(typeName))
        {
            // Already registered - this is normal when called from both static constructor
            // and ForceAutoRegistrationFromCore(). Just ignore silently.
            return;
        }

        _types[typeName] = typeof(T);
    }

    /// <summary>
    /// Creates a new instance of a transformation by type name, initialized with parent project.
    /// </summary>
    /// <param name="typeName">The name of the transformation type</param>
    /// <param name="parentProject">The project that owns this transformation</param>
    /// <returns>A new, initialized instance of the transformation</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type is not registered or factory not set</exception>
    public static Entities.TransformationBase Create(string typeName, Entities.ProjectBase parentProject)
    {
        ArgumentNullException.ThrowIfNull(parentProject);

        EnsureInitialized();

        if (_factory == null)
            throw new InvalidOperationException("Factory not set. Call SetFactory before creating transformations.");

        if (!_types.TryGetValue(typeName, out var type))
        {
            throw new InvalidOperationException(
                $"Unknown transformation type: '{typeName}'. Make sure it's registered.");
        }

        var transformation = _factory(type);
        transformation.Initialize(parentProject, Guid.NewGuid());
        return transformation;
    }

    /// <summary>
    /// Gets all registered transformation types.
    /// </summary>
    /// <returns>Collection of all registered transformation types</returns>
    public static IReadOnlyCollection<Type> GetAllTypes()
    {
        EnsureInitialized();
        return _types.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the type by name.
    /// </summary>
    /// <param name="typeName">The name of the transformation type</param>
    /// <returns>The Type if found, null otherwise</returns>
    public static Type? GetTypeByName(string typeName)
    {
        EnsureInitialized();
        _types.TryGetValue(typeName, out var type);
        return type;
    }

    /// <summary>
    /// Checks if a transformation type is registered.
    /// </summary>
    /// <param name="typeName">The name of the transformation type</param>
    /// <returns>True if registered, false otherwise</returns>
    public static bool IsRegistered(string typeName)
    {
        EnsureInitialized();
        return _types.ContainsKey(typeName);
    }

    /// <summary>
    /// Gets the icon for a transformation type by name.
    /// Uses reflection to access the static IconResourceName property and loads the embedded resource.
    /// </summary>
    /// <param name="typeName">The name of the transformation type</param>
    /// <returns>The icon as PNG ImageData, or null if not found</returns>
    public static ImageData? GetIcon(string typeName)
    {
        var type = GetTypeByName(typeName);
        if (type == null || !type.IsAssignableTo(typeof(Entities.ITransformationMetadata)))
            return null;

        try
        {
            // Access the static IconResourceName property via reflection
            var property = type.GetProperty(
                nameof(Entities.ITransformationMetadata.IconResourceName),
                BindingFlags.Public | BindingFlags.Static);

            if (property == null)
                return null;

            var resourceName = (string?)property.GetValue(null);
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;

            return Helpers.EmbeddedResourceLoader.LoadIcon(resourceName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Registers all available transformation types.
    /// This method should be called once at application startup.
    /// Add new transformation types here as they are implemented.
    /// </summary>
    public static void RegisterAll()
    {
        if (_isInitialized)
            return;

        // Force auto-registration by loading all transformation types
        ForceAutoRegistrationFromCore();

        _isInitialized = true;
    }

    /// <summary>
    /// Forces auto-registration by triggering static constructors of all TransformationBase subclasses.
    /// Scans the Core assembly for transformation types and initializes them.
    /// </summary>
    public static void ForceAutoRegistrationFromCore()
    {
        var assembly = typeof(Entities.TransformationBase).Assembly;

        var transformationTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Entities.TransformationBase)));

        foreach (var type in transformationTypes)
        {
            // Trigger static constructor by accessing a static member
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }
    }

    /// <summary>
    /// Clears all registrations. Primarily for testing purposes.
    /// </summary>
    internal static void Clear()
    {
        _types.Clear();
        _isInitialized = false;
    }

    private static void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            RegisterAll();
        }
    }
}
