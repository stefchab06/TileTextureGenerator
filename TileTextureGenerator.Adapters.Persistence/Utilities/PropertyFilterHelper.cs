using System.Reflection;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Adapters.Persistence.Utilities;

/// <summary>
/// Helper for filtering properties during serialization.
/// Used primarily for archiving to serialize only base class properties.
/// </summary>
public static class PropertyFilterHelper
{
    /// <summary>
    /// Determines if a property is declared in the base class (ProjectBase or TransformationBase).
    /// </summary>
    /// <param name="property">Property to check.</param>
    /// <returns>True if property is declared in base class, false otherwise.</returns>
    public static bool IsBaseProperty(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var declaringType = property.DeclaringType;
        return declaringType == typeof(ProjectBase) || 
               declaringType == typeof(TransformationBase);
    }

    /// <summary>
    /// Gets properties to serialize for archiving a project (base class properties only).
    /// Excludes system properties (Name, Type, ParentProject, Icon, AvailableActions).
    /// Excludes SourceImage (keep DisplayImage only for archiving).
    /// </summary>
    /// <param name="project">Project instance to get properties from.</param>
    /// <returns>Enumerable of properties suitable for archiving.</returns>
    public static IEnumerable<PropertyInfo> GetArchivableProjectProperties(ProjectBase project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && 
                        p.GetIndexParameters().Length == 0 &&
                        IsBaseProperty(p) &&
                        !IsSystemProperty(p.Name) &&
                        !IsArchiveExcludedProjectProperty(p.Name));
    }

    /// <summary>
    /// Gets properties to serialize for archiving a transformation (base class properties only).
    /// Excludes system properties and archive-excluded properties.
    /// </summary>
    /// <param name="transformation">Transformation instance to get properties from.</param>
    /// <returns>Enumerable of properties suitable for archiving.</returns>
    public static IEnumerable<PropertyInfo> GetArchivableTransformationProperties(TransformationBase transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);

        return transformation.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && 
                        p.GetIndexParameters().Length == 0 &&
                        IsBaseProperty(p) &&
                        !IsSystemProperty(p.Name) &&
                        !IsArchiveExcludedTransformationProperty(p.Name));
    }

    /// <summary>
    /// Checks if a property is a system property that should never be serialized.
    /// System properties: ParentProject, AvailableActions, Name, Type, Id.
    /// Note: Icon property was removed from TransformationBase (replaced by static IconResourceName).
    /// </summary>
    private static bool IsSystemProperty(string propertyName)
    {
        return propertyName is "ParentProject" or "AvailableActions" or "Name" or "Type" or "Id";
    }

    /// <summary>
    /// Checks if a property should be excluded from project archiving.
    /// Currently excludes: SourceImage (keep DisplayImage only).
    /// </summary>
    private static bool IsArchiveExcludedProjectProperty(string propertyName)
    {
        return propertyName == "SourceImage";
    }

    /// <summary>
    /// Checks if a property should be excluded from transformation archiving.
    /// Currently no specific exclusions (EdgeFlap is in concrete classes, already filtered by IsBaseProperty).
    /// </summary>
    private static bool IsArchiveExcludedTransformationProperty(string propertyName)
    {
        // No additional exclusions for now
        // EdgeFlap and other concrete properties are already filtered by IsBaseProperty check
        return false;
    }
}
