# Automatic Dependency Injection System

## Overview

This system allows automatic registration of dependencies in the .NET MAUI DI container without having to manually write each registration. It scans assemblies and automatically registers all interfaces that have a single implementation.

## How It Works

### 1. Automatic Registration by Assemblies

In `MauiProgram.cs`, dependencies are automatically registered:

```csharp
services.RegisterDependenciesFromAssemblies(assemblies);
```

### 2. Naming Conventions

The system automatically determines the lifetime based on conventions:

| Class Name Suffix | Lifetime | Example |
|-------------------|----------|---------|
| `ViewModel` | Transient | `ProjectSelectorViewModel` |
| `Controller` | Transient | `ProjectSelectorController` |
| `UseCase` | Scoped | `ProjectSelectionUseCase` |
| `Repository` | Singleton | `ProjectRepository` |
| `Store` | Singleton | `TextureProjectStore` |
| `Service` | Singleton | `LocalizationService` |
| `Persister` | Singleton | `ProjectPersister` |
| Others | Transient | (default) |

### 3. Attributes for Manual Control (Optional)

You can use attributes for precise control:

```csharp
// Force a specific lifetime
[AutoRegister(ServiceLifetime.Singleton)]
public class MyService : IMyService
{
    // ...
}

// Exclude from automatic registration
[SkipAutoRegister]
public class UnregisteredClass : IInterface
{
    // ...
}
```

## Usage in Your Classes

Once configured, dependencies are automatically injected:

```csharp
public partial class ProjectSelectorViewModel : ObservableObject
{
    private readonly IProjectSelectionUseCase _projectSelectionUseCase;

    // Injection happens automatically
    public ProjectSelectorViewModel(IProjectSelectionUseCase projectSelectionUseCase)
    {
        _projectSelectionUseCase = projectSelectionUseCase;
    }
}
```

## Important Rules

1. **One implementation per interface**: The system only registers interfaces that have exactly one implementation in the assembly
2. **Excluded namespaces**: System interfaces (System.* and Microsoft.*) are automatically excluded
3. **Concrete types only**: Abstract classes and generic types are ignored

## Debugging

In DEBUG mode, the system displays all registered dependencies in the debug output:

```
=== Starting automatic dependency registration ===
Analyzing assembly: TileTextureGenerator.Core
  ✓ Singleton: ITextureProjectStore -> TextureProjectStore
Analyzing assembly: TileTextureGenerator.Adapters.UseCases
  ✓ Scoped: IProjectSelectionUseCase -> ProjectSelectionUseCase
...
=== Finished automatic dependency registration ===
```

## Alternative: Registration by Prefix

Instead of specifying assemblies, you can use a prefix:

```csharp
// Registers all assemblies starting with "TileTextureGenerator"
services.RegisterDependenciesFromPrefix("TileTextureGenerator", enableLogging: true);
```

## Advantages

✅ Less boilerplate code
✅ No need to manually maintain DI registrations
✅ Automatic detection of new dependencies
✅ Clear and consistent conventions
✅ Easy to debug with logging

## Potential Drawbacks

⚠️ Less explicit (registrations are "hidden")
⚠️ May register unwanted classes (use `[SkipAutoRegister]`)
⚠️ Slight startup overhead (assembly scanning)

