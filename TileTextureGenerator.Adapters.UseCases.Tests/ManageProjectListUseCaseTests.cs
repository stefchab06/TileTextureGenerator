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
            new("P1", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow, new byte[] { 1, 2, 3 }, ProjectActions.Load | ProjectActions.Delete),
            new("P2", "WallTileProject", ProjectStatus.Archived, DateTime.UtcNow, null, ProjectActions.Delete)
        };
        var useCase = new ManageProjectListUseCase(mockManager);
        var result = await useCase.ListProjectsAsync();
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Projects);
        Assert.Equal(2, result.Projects.Count);

        // Verify P1 (New status)
        Assert.Equal("P1", result.Projects[0].Name);
        Assert.Equal("FloorTileProject", result.Projects[0].Type);
        Assert.Equal(ProjectStatus.New, result.Projects[0].Status);
        Assert.NotNull(result.Projects[0].DisplayImage);
        Assert.True(result.Projects[0].CanLoad, "New projects should have CanLoad=true");
        Assert.False(result.Projects[0].CanGenerate, "New projects should have CanGenerate=false");
        Assert.False(result.Projects[0].CanArchive, "New projects should have CanArchive=false");
        Assert.True(result.Projects[0].CanDelete, "New projects should have CanDelete=true");

        // Verify P2 (Archived status)
        Assert.Equal("P2", result.Projects[1].Name);
        Assert.Equal(ProjectStatus.Archived, result.Projects[1].Status);
        Assert.Null(result.Projects[1].DisplayImage);
        Assert.False(result.Projects[1].CanLoad, "Archived projects should have CanLoad=false");
        Assert.False(result.Projects[1].CanGenerate, "Archived projects should have CanGenerate=false");
        Assert.False(result.Projects[1].CanArchive, "Archived projects should have CanArchive=false");
        Assert.True(result.Projects[1].CanDelete, "Archived projects should have CanDelete=true");
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

    [Fact]
    public async Task ListProjectsAsync_MapsPendingStatusActionsCorrectly()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        mockManager.Projects = new List<ProjectDto> {
            new("PendingProject", "FloorTileProject", ProjectStatus.Pending, DateTime.UtcNow, null, 
                ProjectActions.Load | ProjectActions.Generate | ProjectActions.Delete)
        };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.ListProjectsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var project = result.Projects[0];
        Assert.True(project.CanLoad, "Pending projects should have CanLoad=true");
        Assert.True(project.CanGenerate, "Pending projects should have CanGenerate=true");
        Assert.False(project.CanArchive, "Pending projects should have CanArchive=false");
        Assert.True(project.CanDelete, "Pending projects should have CanDelete=true");
    }

    [Fact]
    public async Task ListProjectsAsync_MapsGeneratedStatusActionsCorrectly()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        mockManager.Projects = new List<ProjectDto> {
            new("GeneratedProject", "WallTileProject", ProjectStatus.Generated, DateTime.UtcNow, null, 
                ProjectActions.Load | ProjectActions.Generate | ProjectActions.Archive | ProjectActions.Delete)
        };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.ListProjectsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var project = result.Projects[0];
        Assert.True(project.CanLoad, "Generated projects should have CanLoad=true");
        Assert.True(project.CanGenerate, "Generated projects should have CanGenerate=true");
        Assert.True(project.CanArchive, "Generated projects should have CanArchive=true");
        Assert.True(project.CanDelete, "Generated projects should have CanDelete=true");
    }

    [Fact]
    public async Task ListProjectsAsync_MapsUnexistingStatusActionsCorrectly()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        mockManager.Projects = new List<ProjectDto> {
            new("UnexistingProject", "FloorTileProject", ProjectStatus.Unexisting, DateTime.UtcNow, null, 
                ProjectActions.Delete)
        };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.ListProjectsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var project = result.Projects[0];
        Assert.False(project.CanLoad, "Unexisting projects should have CanLoad=false");
        Assert.False(project.CanGenerate, "Unexisting projects should have CanGenerate=false");
        Assert.False(project.CanArchive, "Unexisting projects should have CanArchive=false");
        Assert.True(project.CanDelete, "Unexisting projects should have CanDelete=true");
    }

    [Fact]
    public async Task ListProjectsAsync_MapsNoActionsCorrectly()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        mockManager.Projects = new List<ProjectDto> {
            new("NoActionsProject", "FloorTileProject", ProjectStatus.New, DateTime.UtcNow, null, 
                ProjectActions.None)
        };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.ListProjectsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var project = result.Projects[0];
        Assert.False(project.CanLoad, "Projects with no actions should have CanLoad=false");
        Assert.False(project.CanGenerate, "Projects with no actions should have CanGenerate=false");
        Assert.False(project.CanArchive, "Projects with no actions should have CanArchive=false");
        Assert.False(project.CanDelete, "Projects with no actions should have CanDelete=false");
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidParameters_ReturnsEditUseCaseNotNull()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("TestProject", "FloorTileProject");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.EditUseCase);
        Assert.NotNull(result.EditUseCase.Project);
        Assert.Equal("TestProject", result.EditUseCase.Project.Name);
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidationError_EditUseCaseIsNull()
    {
        // Arrange
        var mockManager = new MockProjectsManager();
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.EditUseCase);
        Assert.Equal(CreateProjectErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateProjectAsync_WithUnexpectedException_EditUseCaseIsNull()
    {
        // Arrange
        var mockManager = new MockProjectsManager { ThrowGenericException = true };
        var useCase = new ManageProjectListUseCase(mockManager);

        // Act
        var result = await useCase.CreateProjectAsync("TestProject", "FloorTileProject");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.EditUseCase);
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

        public Task ArchiveProjectAsync(string projectName)
        {
            // Simple stub for testing
            return Task.CompletedTask;
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

        public Task ArchiveAsync(ProjectBase project)
        {
            return Task.CompletedTask;
        }
    }
    #endregion
}
