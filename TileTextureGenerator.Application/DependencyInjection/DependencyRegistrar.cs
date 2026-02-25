using System.Reflection;

namespace TileTextureGenerator.Application.DependencyInjection;

public static class DependencyRegistrar
{
    /// <summary>
    /// Automatically registers all interfaces and their implementations
    /// from the specified assemblies
    /// </summary>
    public static IServiceCollection RegisterDependenciesFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return RegisterDependenciesFromAssemblies(services, enableLogging: false, assemblies);
    }

    /// <summary>
    /// Automatically registers all interfaces and their implementations
    /// from the specified assemblies with logging option
    /// </summary>
    public static IServiceCollection RegisterDependenciesFromAssemblies(
        this IServiceCollection services,
        bool enableLogging,
        params Assembly[] assemblies)
    {
        if (enableLogging)
        {
            System.Diagnostics.Debug.WriteLine("=== Starting automatic dependency registration ===");
        }

        foreach (var assembly in assemblies)
        {
            if (enableLogging)
            {
                System.Diagnostics.Debug.WriteLine($"Analyzing assembly: {assembly.GetName().Name}");
            }
            RegisterFromAssembly(services, assembly, enableLogging);
        }

        if (enableLogging)
        {
            System.Diagnostics.Debug.WriteLine("=== Finished automatic dependency registration ===");
        }

        return services;
    }

    /// <summary>
    /// Automatically registers all interfaces and their implementations
    /// from assemblies whose names start with the specified prefix
    /// </summary>
    public static IServiceCollection RegisterDependenciesFromPrefix(
        this IServiceCollection services,
        string assemblyPrefix,
        bool enableLogging = false)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        if (enableLogging)
        {
            System.Diagnostics.Debug.WriteLine($"Found {assemblies.Count} assemblies with prefix '{assemblyPrefix}'");
        }

        foreach (var assembly in assemblies)
        {
            RegisterFromAssembly(services, assembly, enableLogging);
        }

        return services;
    }

    private static void RegisterFromAssembly(IServiceCollection services, Assembly assembly, bool enableLogging = false)
    {
        // Get all concrete types (non-abstract, non-interfaces)
        var concreteTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => !t.GetCustomAttributes<SkipAutoRegisterAttribute>().Any()) // Exclude marked types
            .ToList();

        foreach (var concreteType in concreteTypes)
        {
            // Find all implemented interfaces (except system interfaces)
            var interfaces = concreteType.GetInterfaces()
                .Where(i => !i.Namespace?.StartsWith("System") == true)
                .Where(i => !i.Namespace?.StartsWith("Microsoft") == true)
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                // Check if this interface has only one implementation
                var implementationsCount = concreteTypes.Count(t => interfaceType.IsAssignableFrom(t));

                if (implementationsCount == 1)
                {
                    // Determine lifetime based on attribute or namespace
                    var lifetime = GetLifetimeFromAttribute(concreteType) ?? DetermineLifetime(concreteType);

                    switch (lifetime)
                    {
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(interfaceType, concreteType);
                            if (enableLogging)
                            {
                                System.Diagnostics.Debug.WriteLine($"  ✓ Singleton: {interfaceType.Name} -> {concreteType.Name}");
                            }
                            break;
                        case ServiceLifetime.Scoped:
                            services.AddScoped(interfaceType, concreteType);
                            if (enableLogging)
                            {
                                System.Diagnostics.Debug.WriteLine($"  ✓ Scoped: {interfaceType.Name} -> {concreteType.Name}");
                            }
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(interfaceType, concreteType);
                            if (enableLogging)
                            {
                                System.Diagnostics.Debug.WriteLine($"  ✓ Transient: {interfaceType.Name} -> {concreteType.Name}");
                            }
                            break;
                    }
                }
            }
        }
    }

    private static ServiceLifetime? GetLifetimeFromAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<AutoRegisterAttribute>();
        return attribute?.Lifetime;
    }

    private static ServiceLifetime DetermineLifetime(Type type)
    {
        // Convention: ViewModels are Transient
        if (type.Name.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceLifetime.Transient;
        }

        // Convention: Controllers are Transient
        if (type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceLifetime.Transient;
        }

        // Convention: Repositories, Stores, and Services are Singleton
        if (type.Name.EndsWith("Repository", StringComparison.OrdinalIgnoreCase) ||
            type.Name.EndsWith("Store", StringComparison.OrdinalIgnoreCase) ||
            type.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase) ||
            type.Name.EndsWith("Persister", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceLifetime.Singleton;
        }

        // Convention: UseCases are Scoped (or Transient depending on preference)
        if (type.Name.EndsWith("UseCase", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceLifetime.Scoped;
        }

        // Default: Transient (safest option)
        return ServiceLifetime.Transient;
    }
}
