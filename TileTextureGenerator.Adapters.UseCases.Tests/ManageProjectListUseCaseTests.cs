using TileTextureGenerator.Adapters.UseCases;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using Xunit;

namespace TileTextureGenerator.Adapters.UseCases.Tests;

public class ManageProjectListUseCaseTests
{
    [Fact]
    public async Task LoadProjectTypesAsync_ReturnsListFromProjectsManager()
    {
        // Arrange
        var expectedTypes = new List<string> { "FloorTileProject", "WallTileProject" };
        var mockManager = new MockProjectsManager { ProjectTypes = expectedTypes };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.LoadProjectTypesAsync();

        // Assert
        Assert.Equal(expectedTypes.Count, result.Count);
        Assert.Equal(expectedTypes[0], result[0]);
        Assert.Equal(expectedTypes[1], result[1]);
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidParameters_ReturnsSuccessResult()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("TestProject", "FloorTileProject");

        // Assert - Add detailed error message if it fails
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorMessage} (Type: {result.ErrorType})");
        Assert.NotNull(result.CreatedProject);
        Assert.Equal("TestProject", result.CreatedProject.Name);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.None, result.ErrorType);
    }

    [Fact]
    public async Task CreateProjectAsync_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateProjectAsync_WithWhitespaceName_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("   ", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateProjectAsync_WithEmptyType_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("TestProject", "");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateProjectAsync_WithInvalidType_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowArgumentException = true };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("TestProject", "InvalidType");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateProjectAsync_WhenProjectAlreadyExists_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowInvalidOperationException = true };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("ExistingProject", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateProjectAsync_WithUnexpectedException_ReturnsUnexpectedError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowGenericException = true };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("TestProject", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.CreatedProject);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(CreateProjectErrorType.Unexpected, result.ErrorType);
    }

    [Fact]
    public async Task ProjectExistsAsync_WithNullOrWhitespaceName_ReturnsFalse()
    {
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);
        Assert.False(await useCase.ProjectExistsAsync(null));
        Assert.False(await useCase.ProjectExistsAsync(""));
        Assert.False(await useCase.ProjectExistsAsync("   "));
    }

    [Fact]
    public async Task ProjectExistsAsync_WithValidName_DelegatesToManager()
    {
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);
        // Default mock returns false
        Assert.False(await useCase.ProjectExistsAsync("Test"));
        // Simulate project exists
        mockManager.ProjectExistsResult = true;
        Assert.True(await useCase.ProjectExistsAsync("Test"));
    }

    [Fact]
    public async Task ListProjectsAsync_ReturnsMappedList()
    {
        var mockManager = new MockProjectsManager();
        mockManager.Projects = new List<ProjectDto> {
            new("P1", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow, new byte[] { 1, 2, 3 }),
            new("P2", "WallTileProject", ProjectStatus.Archived, DateTime.UtcNow, null)
        };
        var useCase = new ManageProjectListUseCase(mockManager);
        var result = await useCase.ListProjectsAsync();
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Projects);
        Assert.Equal(2, result.Projects.Count);
        Assert.Equal("P1", result.Projects[0].Name);
        Assert.Equal("FloorTileProject", result.Projects[0].Type);
        Assert.Equal(ProjectStatus.New, result.Projects[0].Status);
        Assert.NotNull(result.Projects[0].DisplayImage);
        Assert.Equal("P2", result.Projects[1].Name);
        Assert.Equal(ProjectStatus.Archived, result.Projects[1].Status);
        Assert.Null(result.Projects[1].DisplayImage);
    }

    [Fact]
    public async Task ListProjectsAsync_WhenManagerThrows_ReturnsError()
    {
        var mockManager = new MockProjectsManager { ThrowGenericException = true };
        var useCase = new ManageProjectListUseCase(mockManager);
        var result = await useCase.ListProjectsAsync();
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ListProjectsAsync_WithEmptyList_ReturnsEmptyList()
    {
        var mockManager = new MockProjectsManager { Projects = new List<ProjectDto>() };
        var useCase = new ManageProjectListUseCase(mockManager);
        var result = await useCase.ListProjectsAsync();
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Projects);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public async Task DeleteProjectAsync_WithValidName_ReturnsSuccess()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.DeleteProjectAsync("TestProject");

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorMessage}");
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProjectAsync_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.DeleteProjectAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProjectAsync_WithWhitespaceName_ReturnsValidationError()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.DeleteProjectAsync("   ");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProjectAsync_WhenProjectNotFound_ReturnsError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowDeleteArgumentException = true };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.DeleteProjectAsync("NonExistentProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProjectAsync_WhenOperationFails_ReturnsError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowDeleteInvalidOperationException = true };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.DeleteProjectAsync("TestProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProjectAsync_WithUnexpectedException_ReturnsError()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowDeleteGenericException = true };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.DeleteProjectAsync("TestProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
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
        public bool ThrowDeleteArgumentException { get; set; }
        public bool ThrowDeleteInvalidOperationException { get; set; }
        public bool ThrowDeleteGenericException { get; set; }
        public IReadOnlyList<ProjectDto>? Projects { get; set; }
        public bool ProjectExistsResult { get; set; }

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
            if (ThrowDeleteArgumentException)
                throw new ArgumentException("Project not found");

            if (ThrowDeleteInvalidOperationException)
                throw new InvalidOperationException("Cannot delete project");

            if (ThrowDeleteGenericException)
                throw new Exception("Unexpected delete error");

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ProjectDto>> ListProjectsAsync()
        {
            if (ThrowGenericException)
                throw new Exception("Unexpected error");
            return Task.FromResult<IReadOnlyList<ProjectDto>>(Projects);
        }

        public Task<bool> ProjectExistsAsync(string projectName)
        {
            return Task.FromResult(ProjectExistsResult);
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
