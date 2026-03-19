using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for ProjectsManager service.
/// Tests projects collection lifecycle: list types, create, select, delete, list projects.
/// </summary>
public class ProjectsManagerTests
{
    private const string FakeProjectTypeAName = "FakeProjectTypeA";
    private const string FakeProjectTypeBName = "FakeProjectTypeB";

    public ProjectsManagerTests()
    {
        // Clear registry before each test to ensure isolation
        TextureProjectRegistry.ClearForTesting();

        // Setup fake factory that creates projects with fake stores
        TextureProjectRegistry.SetFactory(type =>
        {
            var store = new FakeProjectStore();
            return (ProjectBase)Activator.CreateInstance(type, store)!;
        });
    }

    private sealed class FakeProjectTypeA : ProjectBase
    {
        public FakeProjectTypeA(IProjectStore store) : base(store)
        {
        }

        public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
        {
            return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>(Array.Empty<TransformationTypeDTO>());
        }
    }

    private sealed class FakeProjectTypeB : ProjectBase
    {
        public FakeProjectTypeB(IProjectStore store) : base(store)
        {
        }

        public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
        {
            return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>(Array.Empty<TransformationTypeDTO>());
        }
    }

    private class FakeProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationID) => Task.CompletedTask;
        public Task<TransformationBase> LoadTransformationAsync(ProjectBase project, Guid transformationId) => Task.FromResult<TransformationBase>(null!);
    }

    private class FakeProjectsStore : IProjectsStore
    {
        private readonly Dictionary<string, ProjectDto> _projects = new();

        public Task CreateProjectAsync(ProjectDto projectDto)
        {
            _projects[projectDto.Name] = projectDto;
            return Task.CompletedTask;
        }

        public Task<ProjectBase?> LoadAsync(string projectName)
        {
            if (_projects.TryGetValue(projectName, out var dto))
            {
                // Create a basic ProjectBase from DTO for testing
                var project = TextureProjectRegistry.Create(dto.Type, dto.Name);
                project.Status = dto.Status;
                project.LastModifiedDate = dto.LastModifiedDate;
                project.DisplayImage = dto.DisplayImage;
                return Task.FromResult<ProjectBase?>(project);
            }
            return Task.FromResult<ProjectBase?>(null);
        }

        public Task<IReadOnlyList<ProjectDto>> ListProjectsAsync()
        {
            return Task.FromResult<IReadOnlyList<ProjectDto>>(_projects.Values.ToList());
        }

        public Task DeleteAsync(string projectName)
        {
            _projects.Remove(projectName);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string projectName)
        {
            return Task.FromResult(_projects.ContainsKey(projectName));
        }
    }

    [Fact]
    public async Task ListProjectTypesAsync_ReturnsRegisteredType()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act
        var types = await manager.ListProjectTypesAsync();

        // Assert
        Assert.NotNull(types);
        Assert.NotEmpty(types);
        Assert.Contains(FakeProjectTypeAName, types);
    }

    [Fact]
    public async Task ListProjectTypesAsync_ReturnsTwoTypes_WhenTwoTypesRegistered()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        TextureProjectRegistry.RegisterType<FakeProjectTypeB>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act
        var types = await manager.ListProjectTypesAsync();

        // Assert
        Assert.NotNull(types);
        Assert.True(types.Count >= 2, $"Expected at least 2 types, got {types.Count}");
        Assert.Contains(FakeProjectTypeAName, types);
        Assert.Contains(FakeProjectTypeBName, types);
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidTypeAndName_CreatesProject()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);
        var projectName = "TestProject";

        // Act
        var project = await manager.CreateProjectAsync(projectName, FakeProjectTypeAName);

        // Assert
        Assert.NotNull(project);
        Assert.Equal(projectName, project.Name);
        Assert.Equal(FakeProjectTypeAName, project.Type);
        Assert.Equal(ProjectStatus.New, project.Status);
    }

    [Fact]
    public async Task CreateProjectAsync_WithInvalidType_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.CreateProjectAsync("Test", "InvalidType"));
    }

    [Fact]
    public async Task CreateProjectAsync_WithExistingName_ThrowsInvalidOperationException()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);
        var projectName = "DuplicateProject";

        await manager.CreateProjectAsync(projectName, FakeProjectTypeAName);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.CreateProjectAsync(projectName, FakeProjectTypeAName));
    }

    [Fact]
    public async Task ListProjectsAsync_ReturnsEmptyList_WhenNoProjects()
    {
        // Arrange
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task ListProjectsAsync_ReturnsAllProjects_AfterCreation()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        TextureProjectRegistry.RegisterType<FakeProjectTypeB>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        await manager.CreateProjectAsync("Project1", FakeProjectTypeAName);
        await manager.CreateProjectAsync("Project2", FakeProjectTypeBName);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.Name == "Project1" && p.Type == FakeProjectTypeAName);
        Assert.Contains(projects, p => p.Name == "Project2" && p.Type == FakeProjectTypeBName);
    }

    [Fact]
    public async Task SelectProjectAsync_WithExistingProject_ReturnsProject()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);
        var projectName = "ExistingProject";

        await manager.CreateProjectAsync(projectName, FakeProjectTypeAName);

        // Act
        var project = await manager.SelectProjectAsync(projectName);

        // Assert
        Assert.NotNull(project);
        Assert.Equal(projectName, project.Name);
    }

    [Fact]
    public async Task SelectProjectAsync_WithNonExistingProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.SelectProjectAsync("NonExistent"));
    }

    [Fact]
    public async Task DeleteProjectAsync_RemovesProject()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);
        var projectName = "ProjectToDelete";

        await manager.CreateProjectAsync(projectName, FakeProjectTypeAName);

        // Act
        await manager.DeleteProjectAsync(projectName);

        // Assert
        var projects = await manager.ListProjectsAsync();
        Assert.DoesNotContain(projects, p => p.Name == projectName);
    }

    [Fact]
    public async Task DeleteProjectAsync_WithNonExistingProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.DeleteProjectAsync("NonExistent"));
    }
}
