using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Services;

/// <summary>
/// Service managing the project lifecycle: creation, selection, deletion, and listing.
/// Implements the IProjectsManager input port for UI adapters.
/// </summary>
public class ProjectsManager : IProjectsManager
{
    private readonly IProjectsStore _projectsStore;

    public ProjectsManager(IProjectsStore projectStore)
    {
        ArgumentNullException.ThrowIfNull(projectStore);
        _projectsStore = projectStore;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListProjectTypesAsync()
    {
        return await Task.FromResult(TextureProjectRegistry.GetRegisteredTypes());
    }

    /// <inheritdoc />
    public async Task<ProjectBase> CreateProjectAsync(string name, string type)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(type);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Project type cannot be empty or whitespace.", nameof(type));

        // Validate that the type is registered
        if (!TextureProjectRegistry.IsRegistered(type))
            throw new ArgumentException($"Project type '{type}' is not registered.", nameof(type));

        // Check if project already exists
        if (await _projectsStore.ExistsAsync(name))
            throw new InvalidOperationException($"A project with name '{name}' already exists.");

        // Create new project using the registry factory
        var project = TextureProjectRegistry.Create(type, name);
        project.Status = ProjectStatus.New;

        // Persist the new project
        await _projectsStore.SaveAsync(project);

        return project;
    }

    /// <inheritdoc />
    public async Task<ProjectBase> SelectProjectAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(name));

        var project = await _projectsStore.LoadAsync(name);

        if (project == null)
            throw new InvalidOperationException($"Project '{name}' not found.");

        return project;
    }

    /// <inheritdoc />
    public async Task DeleteProjectAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(name));

        // Verify project exists before attempting deletion
        if (!await _projectsStore.ExistsAsync(name))
            throw new InvalidOperationException($"Project '{name}' not found.");

        await _projectsStore.DeleteAsync(name);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectDto>> ListProjectsAsync()
    {
        return await _projectsStore.ListProjectsAsync();
    }
}
