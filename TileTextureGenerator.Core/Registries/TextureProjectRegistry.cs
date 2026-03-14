using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Registries;

/// <summary>
/// Registry for project types with factory support for DI integration.
/// Maps project type names to their Type information for dynamic instantiation.
/// </summary>
public static class TextureProjectRegistry
{
    private static readonly Dictionary<string, Type> _registeredTypes = new(StringComparer.Ordinal);
    private static Func<Type, ProjectBase>? _factory;

    /// <summary>
    /// Sets the factory function that creates project instances.
    /// This factory is typically provided by the DI container.
    /// Must be called during application startup before creating any projects.
    /// </summary>
    /// <param name="factory">Factory function that resolves and creates project instances.</param>
    public static void SetFactory(Func<Type, ProjectBase> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Registers a project type for polymorphic instantiation.
    /// </summary>
    /// <typeparam name="TProject">The project type to register.</typeparam>
    public static void RegisterType<TProject>() where TProject : ProjectBase
    {
        var key = typeof(TProject).Name;
        _registeredTypes[key] = typeof(TProject);
    }

    /// <summary>
    /// Creates a project instance using the registered factory and initializes it with the given name.
    /// </summary>
    /// <param name="key">The project type key (typically the class name).</param>
    /// <param name="name">The project name for initialization.</param>
    /// <returns>The created and initialized project.</returns>
    public static ProjectBase Create(string key, string name)
    {
        if (_factory == null)
            throw new InvalidOperationException("Factory not set. Call SetFactory during application startup.");

        if (!_registeredTypes.TryGetValue(key, out var projectType))
        {
            var known = _registeredTypes.Count == 0
                ? "(no registered types)"
                : string.Join(", ", _registeredTypes.Keys.OrderBy(k => k));

            throw new KeyNotFoundException(
                $"No project type registered for key '{key}'. Known keys: {known}"
            );
        }

        var project = _factory(projectType);
        project.Initialize(name);
        return project;
    }

            /// <summary>
            /// Indicates whether a project type is registered for the given key.
            /// </summary>
            public static bool IsRegistered(string key) => _registeredTypes.ContainsKey(key);

            /// <summary>
            /// Returns all registered project type keys.
            /// </summary>
            public static IReadOnlyList<string> GetRegisteredTypes() => _registeredTypes.Keys.ToList();

            /// <summary>
            /// Force auto registration by triggering the static constructors of all non-abstract classes 
            /// in the given assembly that derive from ProjectBase.
            /// </summary>
            public static void ForceAutoRegistration(Assembly assembly)
            {
                if (assembly is null)
                    throw new ArgumentNullException(nameof(assembly));

                var projectTypes = assembly
                    .GetTypes()
                    .Where(t =>
                        t is { IsAbstract: false, IsClass: true } &&
                        typeof(ProjectBase).IsAssignableFrom(t));

                foreach (var type in projectTypes)
                {
                    // Trigger the static constructor if present
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
            }

            /// <summary>
            /// To be called from another namespace
            /// </summary>
            public static void ForceAutoRegistrationFromCore()
            {
                ForceAutoRegistration(typeof(ProjectBase).Assembly);
            }

                /// <summary>
                /// Clears all registered factories. Internal method for testing purposes only.
                /// Accessible from test projects via InternalsVisibleTo.
                /// Does NOT clear the factory - only registered types.
                /// </summary>
                internal static void ClearForTesting()
                {
                    _registeredTypes.Clear();
                    // Keep _factory intact for tests
                }
            }

