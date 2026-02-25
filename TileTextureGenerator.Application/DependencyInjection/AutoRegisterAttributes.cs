namespace TileTextureGenerator.Application.DependencyInjection;

/// <summary>
/// Marks a class for automatic registration in DI
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoRegisterAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; set; }

    public AutoRegisterAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }
}

/// <summary>
/// Excludes a class from automatic registration
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SkipAutoRegisterAttribute : Attribute
{
}
