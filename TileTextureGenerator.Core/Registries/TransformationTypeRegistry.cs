using System.Reflection;
using TileTextureGenerator.Core.Attributes;
using TileTextureGenerator.Core.Transformations;

namespace TileTextureGenerator.Core.Registries;

/// <summary>
/// Registry for transformation types.
/// Manages registration, instantiation, and metadata for all available transformation types.
/// </summary>
public static class TransformationTypeRegistry
{
    private static readonly Dictionary<string, Type> _types = new();
    private static bool _isInitialized = false;

    /// <summary>
    /// Registers a transformation type.
    /// </summary>
    /// <typeparam name="T">The transformation type to register</typeparam>
    public static void Register<T>() where T : TransformationBase
    {
        var typeName = typeof(T).Name;
        
        if (_types.ContainsKey(typeName))
        {
            throw new InvalidOperationException(
                $"Transformation type '{typeName}' is already registered.");
        }

        _types[typeName] = typeof(T);
    }

    /// <summary>
    /// Creates a new instance of a transformation by type name.
    /// </summary>
    /// <param name="typeName">The name of the transformation type</param>
    /// <returns>A new instance of the transformation</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type is not registered</exception>
    public static TransformationBase Create(string typeName)
    {
        EnsureInitialized();

        if (!_types.TryGetValue(typeName, out var type))
        {
            throw new InvalidOperationException(
                $"Unknown transformation type: '{typeName}'. Make sure it's registered in RegisterAll().");
        }

        var instance = (TransformationBase)Activator.CreateInstance(type)!;
        return instance;
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
    /// Gets the maximum cardinality for a transformation type.
    /// </summary>
    /// <param name="type">The transformation type</param>
    /// <returns>Maximum number of instances allowed per project</returns>
    public static int GetMaxCardinality(Type type)
    {
        var attr = type.GetCustomAttribute<TransformationCardinalityAttribute>();
        return attr?.MaxPerProject ?? int.MaxValue;
    }

    /// <summary>
    /// Gets the maximum cardinality for a transformation type by name.
    /// </summary>
    /// <param name="typeName">The name of the transformation type</param>
    /// <returns>Maximum number of instances allowed per project</returns>
    public static int GetMaxCardinality(string typeName)
    {
        var type = GetTypeByName(typeName);
        return type != null ? GetMaxCardinality(type) : int.MaxValue;
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
    /// Gets the count of instances of a specific type in a project.
    /// </summary>
    /// <param name="transformations">List of transformations in the project</param>
    /// <param name="typeName">The transformation type name to count</param>
    /// <returns>Number of instances of that type</returns>
    public static int GetInstanceCount(
        IEnumerable<Entities.TransformationEntity> transformations,
        string typeName)
    {
        return transformations.Count(t => t.TransformationType == typeName);
    }

    /// <summary>
    /// Checks if adding another instance of a type would exceed the cardinality limit.
    /// </summary>
    /// <param name="transformations">List of transformations in the project</param>
    /// <param name="typeName">The transformation type name to check</param>
    /// <returns>True if another instance can be added, false if limit reached</returns>
    public static bool CanAddInstance(
        IEnumerable<Entities.TransformationEntity> transformations,
        string typeName)
    {
        var currentCount = GetInstanceCount(transformations, typeName);
        var maxCardinality = GetMaxCardinality(typeName);
        return currentCount < maxCardinality;
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
        var assembly = typeof(TransformationBase).Assembly;

        var transformationTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(TransformationBase)));

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
