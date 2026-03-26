using TileTextureGenerator.Adapters.UseCases;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using Xunit;

namespace TileTextureGenerator.Adapters.UseCases.Tests;

/// <summary>
/// Tests for CreateProjectUseCase.
/// Verifies project creation workflow orchestration.
/// </summary>
public class CreateProjectUseCaseTests
{
    [Fact]
    public async Task LoadProjectTypesAsync_ReturnsListFromProjectsManager()
    {
        // Arrange
        var expectedTypes = new List<string> { "FloorTileProject", "WallTileProject" };
        var mockManager = new MockProjectsManager { ProjectTypes = expectedTypes };
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.LoadProjectTypesAsync();

        // Assert
        Assert.Equal(expectedTypes.Count, result.Count);
        Assert.Equal(expectedTypes[0], result[0]);
        Assert.Equal(expectedTypes[1], result[1]);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidParameters_ReturnsSuccessResult()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.ExecuteAsync("TestProject", "FloorTileProject");

        // Assert - Add detailed error message if it fails
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorMessage} (Type: {result.ErrorType})");
        Assert.NotNull(result.CreatedProject);
        Assert.Equal("TestProject", result.CreatedProject.Name);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.None, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.ExecuteAsync("", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_WithWhitespaceName_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.ExecuteAsync("   ", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyType_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.ExecuteAsync("TestProject", "");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidType_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowArgumentException = true };
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.ExecuteAsync("TestProject", "InvalidType");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProjectAlreadyExists_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowInvalidOperationException = true };
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.ExecuteAsync("ExistingProject", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnexpectedException_ReturnsUnexpectedError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowGenericException = true };
        var useCase = new CreateProjectUseCase(mockManager);

        // Act
        var result = await useCase.ExecuteAsync("TestProject", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Unexpected, result.ErrorType);
    }

    #region Mock Implementation

    /// <summary>
    /// Mock implementation of IProjectsManager for testing.
    /// </summary>
    private class MockProjectsManager : IProjectsManager
    {
        public List<string> ProjectTypes { get; set; } = ["FloorTileProject", "WallTileProject"];
        public bool ThrowArgumentException { get; set; }
        public bool ThrowInvalidOperationException { get; set; }
        public bool ThrowGenericException { get; set; }

        public Task<IReadOnlyList<string>> ListProjectTypesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(ProjectTypes);
        }

        public Task<ProjectBase> CreateProjectAsync(string name, string type)
        {
            if (ThrowArgumentException)
                throw new ArgumentException("Invalid type");

            if (ThrowInvalidOperationException)
                throw new InvalidOperationException("Project already exists");

            if (ThrowGenericException)
                throw new Exception("Unexpected error");

            // Create a mock project
            var project = new MockProject();
            project.Initialize(name);
            return Task.FromResult<ProjectBase>(project);
        }

        public Task<ProjectBase> SelectProjectAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task DeleteProjectAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<ProjectDto>> ListProjectsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ProjectExistsAsync(string projectName)
        {
            return Task.FromResult(false); // Default: project doesn't exist
        }
    }

    /// <summary>
    /// Mock project for testing (we can't instantiate abstract ProjectBase directly).
    /// </summary>
    private class MockProject : ProjectBase
    {
        public MockProject() : base(new MockProjectStore())
        {
        }

        public override Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }

        public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
        {
            return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>([]);
        }
    }

    /// <summary>
    /// Mock project store for testing.
    /// </summary>
    private class MockProjectStore : Core.Ports.Output.IProjectStore
    {
        public Task SaveAsync(ProjectBase project)
        {
            return Task.CompletedTask;
        }

        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation)
        {
            return Task.CompletedTask;
        }

        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationId)
        {
            return Task.CompletedTask;
        }

        public Task<TransformationBase?> LoadTransformationAsync(ProjectBase project, Guid transformationId)
        {
            return Task.FromResult<TransformationBase?>(null);
        }
    }

    #endregion
}
