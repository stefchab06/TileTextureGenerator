using System;
using System.Collections.Generic;
using System.Text;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Ports.Input;

namespace TileTextureGenerator.Adapters.UseCases;

/// <summary>
/// Use case for project management workflow.
/// Orchestrates loading project types and creating a new project, listing projects, deleting projects and loading one of them.
/// </summary>
public class ManageProjectListUseCase
{
    private readonly IProjectsManager _projectsManager;

    public ManageProjectListUseCase(IProjectsManager projectsManager)
    {
        ArgumentNullException.ThrowIfNull(projectsManager);
        _projectsManager = projectsManager;
    }

    /// <summary>
    /// Loads available project types for UI display.
    /// This list does not change during execution.
    /// </summary>
    /// <returns>List of registered project type identifiers.</returns>
    public async Task<IReadOnlyList<string>> LoadProjectTypesAsync()
    {
        return await _projectsManager.ListProjectTypesAsync();
    }

    /// <summary>
    /// Checks if a project with the specified name already exists.
    /// Used for real-time validation in the UI.
    /// </summary>
    /// <param name="projectName">Name of the project to check.</param>
    /// <returns>True if the project exists, false otherwise.</returns>
    public async Task<bool> ProjectExistsAsync(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return false;

        return await _projectsManager.ProjectExistsAsync(projectName);
    }

    /// <summary>
    /// Executes the project creation workflow.
    /// </summary>
    /// <param name="projectName">Unique name for the new project.</param>
    /// <param name="projectType">Type identifier (e.g., "FloorTileProject").</param>
    /// <returns>Result containing the created project or error information.</returns>
    public async Task<CreateProjectResult> CreateProjectAsync(string projectName, string projectType)
    {
        // Validate inputs first (don't throw, return validation error)
        if (string.IsNullOrWhiteSpace(projectName))
            return CreateProjectResult.ValidationError("Project name cannot be empty or whitespace.");

        if (string.IsNullOrWhiteSpace(projectType))
            return CreateProjectResult.ValidationError("Project type cannot be empty or whitespace.");

        try
        {
            // Step 1: Validate inputs (handled by service)
            // Step 2: Create project via Core service
            var project = await _projectsManager.CreateProjectAsync(projectName, projectType);

            // Step 3: Return success result
            return CreateProjectResult.Success(project);
        }
        catch (ArgumentException ex)
        {
            // Validation errors (invalid name, type, or already exists)
            return CreateProjectResult.ValidationError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Business logic errors (project already exists)
            return CreateProjectResult.ValidationError(ex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected errors
            return CreateProjectResult.UnexpectedError(ex.Message);
        }
    }

    /// <summary>
    /// Lists all existing projects for display in the UI.
    /// </summary>
    /// <returns>Result containing the list of projects or error information.</returns>
    public async Task<ListProjectsResult> ListProjectsAsync()
    {
        try
        {
            var projects = await _projectsManager.ListProjectsAsync();
            var mapped = projects.Select(p => new ProjectListItemDto(
                p.Name,
                p.Type,
                p.Status,
                p.DisplayImage is not null ? p.DisplayImage.Value : null
            )).ToList();
            return ListProjectsResult.Success(mapped);
        }
        catch (Exception ex)
        {
            return ListProjectsResult.Error($"Failed to load projects: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a project by name.
    /// </summary>
    /// <param name="projectName">Name of the project to delete.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<DeleteProjectResult> DeleteProjectAsync(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return DeleteProjectResult.ValidationError("Project name cannot be empty or whitespace.");

        try
        {
            await _projectsManager.DeleteProjectAsync(projectName);
            return DeleteProjectResult.Success();
        }
        catch (ArgumentException ex)
        {
            return DeleteProjectResult.ValidationError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return DeleteProjectResult.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return DeleteProjectResult.Error($"Unexpected error: {ex.Message}");
        }
    }
}

/// <summary>
/// Result of the CreateProject use case execution.
/// </summary>
public class CreateProjectResult
{
    public bool IsSuccess { get; private init; }
    public ProjectBase? CreatedProject { get; private init; }
    public string? ErrorMessage { get; private init; }
    public CreateProjectErrorType ErrorType { get; private init; }

    private CreateProjectResult() { }

    public static CreateProjectResult Success(ProjectBase project) => new()
    {
        IsSuccess = true,
        CreatedProject = project,
        ErrorType = CreateProjectErrorType.None
    };

    public static CreateProjectResult ValidationError(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        ErrorType = CreateProjectErrorType.Validation
    };

    public static CreateProjectResult UnexpectedError(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        ErrorType = CreateProjectErrorType.Unexpected
    };
}

/// <summary>
/// Types of errors that can occur during project creation.
/// </summary>
public enum CreateProjectErrorType
{
    None,
    Validation,
    Unexpected
}

/// <summary>
/// Result of the DeleteProject use case execution.
/// </summary>
public class DeleteProjectResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }

    private DeleteProjectResult() { }

    public static DeleteProjectResult Success() => new()
    {
        IsSuccess = true
    };

    public static DeleteProjectResult ValidationError(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };

    public static DeleteProjectResult Error(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
}
