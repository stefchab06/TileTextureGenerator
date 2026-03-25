using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Configuration;

/// <summary>
/// Extension methods for automatic dependency injection registration.
/// Scans assemblies and registers services by conventions.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers all services from referenced assemblies using conventions.
    /// Conventions:
    /// - Interfaces starting with 'I' are matched with implementations
    /// - Stores, Repositories, Services are registered as Scoped
    /// - ViewModels are registered as Transient
    /// - Views are registered as Transient
    /// </summary>
    public static IServiceCollection AddAutoRegisteredServices(this IServiceCollection services)
    {
        // Get all assemblies from the current domain that are part of the project
        var assemblies = GetProjectAssemblies();

        // Register services by conventions
        services.AddServicesByConvention(assemblies);

        // Register ViewModels and Views
        services.AddViewModelsAndViews(assemblies);

        return services;
    }

    /// <summary>
    /// Initializes the Core registries with DI factory functions.
    /// Must be called after service registration to enable polymorphic instantiation.
    /// </summary>
    public static IServiceCollection InitializeCoreRegistries(this IServiceCollection services)
    {
        // Build a temporary service provider to get the factory
        var tempProvider = services.BuildServiceProvider();

        // Initialize TextureProjectRegistry with DI factory
        TextureProjectRegistry.SetFactory(type =>
        {
            return (ProjectBase)ActivatorUtilities.CreateInstance(tempProvider, type);
        });

        // Force auto-registration of all project types
        TextureProjectRegistry.ForceAutoRegistration(typeof(ProjectBase).Assembly);

        // Initialize TransformationTypeRegistry with DI factory
        TransformationTypeRegistry.SetFactory(type =>
        {
            return (TransformationBase)ActivatorUtilities.CreateInstance(tempProvider, type);
        });

        // Register all transformation types
        TransformationTypeRegistry.RegisterAll();

        return services;
    }

    private static Assembly[] GetProjectAssemblies()
    {
        // Get assemblies that belong to the TileTextureGenerator solution
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic &&
                       a.FullName != null &&
                       a.FullName.StartsWith("TileTextureGenerator", StringComparison.Ordinal))
            .ToArray();

        return assemblies;
    }

    private static void AddServicesByConvention(this IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition);

            foreach (var implementationType in types)
            {
                // Find interfaces implemented by this type
                var interfaces = implementationType.GetInterfaces()
                    .Where(i => i.Name.StartsWith("I", StringComparison.Ordinal) &&
                               i.Namespace != null &&
                               i.Namespace.StartsWith("TileTextureGenerator", StringComparison.Ordinal));

                foreach (var interfaceType in interfaces)
                {
                    // Determine lifetime based on naming conventions
                    var lifetime = DetermineServiceLifetime(implementationType);

                    // Register the service
                    services.Add(new ServiceDescriptor(interfaceType, implementationType, lifetime));
                }
            }
        }
    }

    private static void AddViewModelsAndViews(this IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var type in types)
            {
                // Register ViewModels (convention: class name ends with ViewModel)
                if (type.Name.EndsWith("ViewModel", StringComparison.Ordinal))
                {
                    services.AddTransient(type);
                }

                // Register Views/Pages (convention: class name ends with Page or View)
                if (type.Name.EndsWith("Page", StringComparison.Ordinal) ||
                    type.Name.EndsWith("View", StringComparison.Ordinal))
                {
                    services.AddTransient(type);
                }
            }
        }
    }

    private static ServiceLifetime DetermineServiceLifetime(Type implementationType)
    {
        var typeName = implementationType.Name.ToLowerInvariant();

        // Stores and Repositories are typically Scoped (per-request in web, per-operation in MAUI)
        if (typeName.Contains("store") || typeName.Contains("repository"))
        {
            return ServiceLifetime.Scoped;
        }

        // Services are typically Scoped
        if (typeName.Contains("service"))
        {
            return ServiceLifetime.Scoped;
        }

        // Use Cases / Command Handlers are typically Scoped
        if (typeName.Contains("usecase") || typeName.Contains("handler"))
        {
            return ServiceLifetime.Scoped;
        }

        // Default to Transient for safety
        return ServiceLifetime.Transient;
    }
}
