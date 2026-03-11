using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Core.Registries
{
    public static class TextureProjectRegistry
    {
        private static readonly Dictionary<string, Func<string, ProjectBase>> _factories
            = new(StringComparer.Ordinal);

        /// <summary>
        /// Register or replace a factory for a given key. The factory should create an instance of a ProjectBase subclass.
        /// </summary>
        public static void Register(string key, Func<string, ProjectBase> factory)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty.", nameof(key));

            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            _factories[key] = factory;
        }

        /// <summary>
        /// Create a ProjectBase instance using the factory associated with the given key. Throws if the key is not found.
        /// </summary>
        public static ProjectBase Create(string key, string name)
        {
            if (!_factories.TryGetValue(key, out var factory))
            {
                var known = _factories.Count == 0
                    ? "(no registered types)"
                    : string.Join(", ", _factories.Keys.OrderBy(k => k));

                throw new KeyNotFoundException(
                    $"No TextureProject registered for key '{key}'. Known keys: {known}"
                );
            }

            return factory(name);
        }

        /// <summary>
        /// Indicates whether a factory is registered for the given key.
        /// </summary>
        public static bool IsRegistered(string key) => _factories.ContainsKey(key);

        /// <summary>
        /// Returns all registered project type keys.
        /// </summary>
        public static IReadOnlyList<string> GetRegisteredTypes() => _factories.Keys.ToList();

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
            /// </summary>
            internal static void ClearForTesting()
            {
                _factories.Clear();
            }
        }
}
