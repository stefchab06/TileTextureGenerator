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

        var status = ProjectStatus.New;

        // Create DTO with basic properties and calculated available actions
        var projectDto = new ProjectDto(
            name: name,
            type: type,
            status: status,
            lastModifiedDate: DateTime.UtcNow,
            displayImage: null,
            availableActions: CalculateAvailableActions(status)
        );

        // Persist the new project via DTO
        await _projectsStore.CreateProjectAsync(projectDto);

        // Create and return the entity instance
        var project = TextureProjectRegistry.Create(type, name);
        project.Status = ProjectStatus.New;

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
        var projects = await _projectsStore.ListProjectsAsync();

        // Recalculate available actions for each project based on current status
        var projectsWithActions = projects.Select(p => new ProjectDto(
            name: p.Name,
            type: p.Type,
            status: p.Status,
            lastModifiedDate: p.LastModifiedDate,
            displayImage: p.DisplayImage,
            availableActions: CalculateAvailableActions(p.Status)
        )).OrderByDescending(p => p.LastModifiedDate).ToList();

        return projectsWithActions;
    }

    /// <inheritdoc />
    public async Task<bool> ProjectExistsAsync(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);

        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(projectName));

        return await _projectsStore.ExistsAsync(projectName);
    }

    /// <summary>
    /// Calculates available actions for a project based on its status.
    /// Business rules:
    /// - Delete: always available
    /// - Load: available when status is New, Pending, or Generated
    /// - Generate: available when status is Pending or Generated
    /// - Archive: available when status is Generated
    /// </summary>
    /// <param name="status">Current project status.</param>
    /// <returns>Flags indicating available actions.</returns>
    private static ProjectActions CalculateAvailableActions(ProjectStatus status)
    {
        var actions = ProjectActions.Delete; // Always available

        if (status is ProjectStatus.New or ProjectStatus.Pending or ProjectStatus.Generated)
            actions |= ProjectActions.Load;

        if (status is ProjectStatus.Pending or ProjectStatus.Generated)
            actions |= ProjectActions.Generate;

        if (status == ProjectStatus.Generated)
            actions |= ProjectActions.Archive;

        return actions;
    }
}
