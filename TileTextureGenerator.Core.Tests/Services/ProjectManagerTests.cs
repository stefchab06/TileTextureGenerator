using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for ProjectManager service.
/// Tests project lifecycle: list types, create, select, delete, list projects.
/// </summary>
public class ProjectManagerTests
{
    private const string FakeProjectTypeAName = "FakeProjectTypeA";
    private const string FakeProjectTypeBName = "FakeProjectTypeB";

    public ProjectManagerTests()
    {
        // Clear registry before each test to ensure isolation
        TextureProjectRegistry.ClearForTesting();
    }

    private sealed class FakeProjectTypeA : TileTextureProjectBase
    {
        public FakeProjectTypeA(string name) : base(name)
        {
            Type = FakeProjectTypeAName;
        }
    }

    private sealed class FakeProjectTypeB : TileTextureProjectBase
    {
        public FakeProjectTypeB(string name) : base(name)
        {
            Type = FakeProjectTypeBName;
        }
    }

    private class FakeTextureProjectStore : ITextureProjectStore
    {
        private readonly Dictionary<string, TileTextureProjectBase> _projects = new();

        public Task SaveAsync(TileTextureProjectBase project)
        {
            _projects[project.Name] = project;
            return Task.CompletedTask;
        }

        public Task<TileTextureProjectBase?> LoadAsync(string projectName)
        {
            _projects.TryGetValue(projectName, out var project);
            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<ProjectDto>> ListProjectsAsync()
        {
            var summaries = _projects.Values
                .Select(p => new ProjectDto(
                    p.Name,
                    p.Type,
                    p.Status,
                    p.LastModifiedDate,
                    p.DisplayImage))
                .ToList();
            return Task.FromResult<IReadOnlyList<ProjectDto>>(summaries);
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
    public async Task ListProjectTypesAsync_ReturnsEmptyList_WhenNoTypesRegistered()
    {
        // Arrange
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

        // Act
        var types = await manager.ListProjectTypesAsync();

        // Assert
        Assert.NotNull(types);
        Assert.Empty(types);
    }

    [Fact]
    public async Task ListProjectTypesAsync_ReturnsOneType_WhenOneTypeRegistered()
    {
        // Arrange
        TextureProjectRegistry.Register(FakeProjectTypeAName, name => new FakeProjectTypeA(name));
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

        // Act
        var types = await manager.ListProjectTypesAsync();

        // Assert
        Assert.NotNull(types);
        Assert.Single(types);
        Assert.Contains(FakeProjectTypeAName, types);
    }

    [Fact]
    public async Task ListProjectTypesAsync_ReturnsTwoTypes_WhenTwoTypesRegistered()
    {
        // Arrange
        TextureProjectRegistry.Register(FakeProjectTypeAName, name => new FakeProjectTypeA(name));
        TextureProjectRegistry.Register(FakeProjectTypeBName, name => new FakeProjectTypeB(name));
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

        // Act
        var types = await manager.ListProjectTypesAsync();

        // Assert
        Assert.NotNull(types);
        Assert.Equal(2, types.Count);
        Assert.Contains(FakeProjectTypeAName, types);
        Assert.Contains(FakeProjectTypeBName, types);
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidTypeAndName_CreatesProject()
    {
        // Arrange
        TextureProjectRegistry.Register(FakeProjectTypeAName, name => new FakeProjectTypeA(name));
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);
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
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.CreateProjectAsync("Test", "InvalidType"));
    }

    [Fact]
    public async Task CreateProjectAsync_WithExistingName_ThrowsInvalidOperationException()
    {
        // Arrange
        TextureProjectRegistry.Register(FakeProjectTypeAName, name => new FakeProjectTypeA(name));
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);
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
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

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
        TextureProjectRegistry.Register(FakeProjectTypeAName, name => new FakeProjectTypeA(name));
        TextureProjectRegistry.Register(FakeProjectTypeBName, name => new FakeProjectTypeB(name));
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

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
        TextureProjectRegistry.Register(FakeProjectTypeAName, name => new FakeProjectTypeA(name));
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);
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
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.SelectProjectAsync("NonExistent"));
    }

    [Fact]
    public async Task DeleteProjectAsync_RemovesProject()
    {
        // Arrange
        TextureProjectRegistry.Register(FakeProjectTypeAName, name => new FakeProjectTypeA(name));
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);
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
        var store = new FakeTextureProjectStore();
        var manager = new ProjectManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.DeleteProjectAsync("NonExistent"));
    }
}
