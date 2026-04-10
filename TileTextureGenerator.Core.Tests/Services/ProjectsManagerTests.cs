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
[Collection("ProjectRegistry")]
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
        public Task ArchiveAsync(ProjectBase project) => Task.CompletedTask;
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
                // DisplayImage cannot be set directly (read-only) - derived classes use SetDisplayImage()
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

    [Fact]
    public async Task ProjectExistsAsync_WithExistingProject_ReturnsTrue()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);
        var projectName = "ExistingProject";

        await manager.CreateProjectAsync(projectName, FakeProjectTypeAName);

        // Act
        var exists = await manager.ProjectExistsAsync(projectName);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ProjectExistsAsync_WithNonExistingProject_ReturnsFalse()
    {
        // Arrange
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act
        var exists = await manager.ProjectExistsAsync("NonExistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ProjectExistsAsync_DelegatesToProjectsStore()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);
        var projectName = "TestProject";

        // Create project in store
        await manager.CreateProjectAsync(projectName, FakeProjectTypeAName);

        // Verify it exists via manager
        var existsBeforeDelete = await manager.ProjectExistsAsync(projectName);

        // Delete and verify
        await manager.DeleteProjectAsync(projectName);
        var existsAfterDelete = await manager.ProjectExistsAsync(projectName);

        // Assert
        Assert.True(existsBeforeDelete, "Project should exist after creation");
        Assert.False(existsAfterDelete, "Project should not exist after deletion");
    }

    [Fact]
    public async Task ProjectExistsAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.ProjectExistsAsync(""));
    }

    [Fact]
    public async Task ProjectExistsAsync_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.ProjectExistsAsync("   "));
    }

    [Fact]
    public async Task ListProjectsAsync_ReturnsSortedByLastModifiedDate_MostRecentFirst()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Create projects with different timestamps (simulate delay between creations)
        var project1 = await manager.CreateProjectAsync("OldProject", FakeProjectTypeAName);
        await Task.Delay(10); // Small delay to ensure different timestamps

        var project2 = await manager.CreateProjectAsync("MiddleProject", FakeProjectTypeAName);
        await Task.Delay(10);

        var project3 = await manager.CreateProjectAsync("NewestProject", FakeProjectTypeAName);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        Assert.Equal(3, projects.Count);
        Assert.Equal("NewestProject", projects[0].Name); // Most recent first
        Assert.Equal("MiddleProject", projects[1].Name);
        Assert.Equal("OldProject", projects[2].Name); // Oldest last
    }

    [Fact]
    public async Task ListProjectsAsync_SortsCorrectly_WithSpecificDates()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();

        // Manually insert projects with specific dates
        var oldDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var middleDate = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var recentDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        await store.CreateProjectAsync(new ProjectDto("Project1", FakeProjectTypeAName, ProjectStatus.New, oldDate, null));
        await store.CreateProjectAsync(new ProjectDto("Project2", FakeProjectTypeAName, ProjectStatus.New, recentDate, null));
        await store.CreateProjectAsync(new ProjectDto("Project3", FakeProjectTypeAName, ProjectStatus.New, middleDate, null));

        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        Assert.Equal(3, projects.Count);
        Assert.Equal("Project2", projects[0].Name); // Most recent (2024-12-31)
        Assert.Equal("Project3", projects[1].Name); // Middle (2024-06-15)
        Assert.Equal("Project1", projects[2].Name); // Oldest (2024-01-01)

        // Verify dates are in descending order
        Assert.True(projects[0].LastModifiedDate >= projects[1].LastModifiedDate);
        Assert.True(projects[1].LastModifiedDate >= projects[2].LastModifiedDate);
    }

    [Fact]
    public async Task ListProjectsAsync_HandlesIdenticalDates()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();

        var sameDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        await store.CreateProjectAsync(new ProjectDto("ProjectA", FakeProjectTypeAName, ProjectStatus.New, sameDate, null));
        await store.CreateProjectAsync(new ProjectDto("ProjectB", FakeProjectTypeAName, ProjectStatus.New, sameDate, null));

        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        Assert.Equal(2, projects.Count);
        // Both dates are identical, order between them is not critical but should be stable
        Assert.Equal(sameDate, projects[0].LastModifiedDate);
        Assert.Equal(sameDate, projects[1].LastModifiedDate);
    }

    [Fact]
    public async Task ListProjectsAsync_CalculatesAvailableActions_ForNewStatus()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        await store.CreateProjectAsync(new ProjectDto("NewProject", FakeProjectTypeAName, ProjectStatus.New, DateTime.UtcNow, null));
        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        var project = projects[0];
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Load), "New projects should allow Load");
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Delete), "New projects should allow Delete");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Generate), "New projects should NOT allow Generate");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Archive), "New projects should NOT allow Archive");
    }

    [Fact]
    public async Task ListProjectsAsync_CalculatesAvailableActions_ForPendingStatus()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        await store.CreateProjectAsync(new ProjectDto("PendingProject", FakeProjectTypeAName, ProjectStatus.Pending, DateTime.UtcNow, null));
        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        var project = projects[0];
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Load), "Pending projects should allow Load");
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Generate), "Pending projects should allow Generate");
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Delete), "Pending projects should allow Delete");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Archive), "Pending projects should NOT allow Archive");
    }

    [Fact]
    public async Task ListProjectsAsync_CalculatesAvailableActions_ForGeneratedStatus()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        await store.CreateProjectAsync(new ProjectDto("GeneratedProject", FakeProjectTypeAName, ProjectStatus.Generated, DateTime.UtcNow, null));
        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        var project = projects[0];
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Load), "Generated projects should allow Load");
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Generate), "Generated projects should allow Generate");
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Archive), "Generated projects should allow Archive");
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Delete), "Generated projects should allow Delete");
    }

    [Fact]
    public async Task ListProjectsAsync_CalculatesAvailableActions_ForArchivedStatus()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        await store.CreateProjectAsync(new ProjectDto("ArchivedProject", FakeProjectTypeAName, ProjectStatus.Archived, DateTime.UtcNow, null));
        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        var project = projects[0];
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Delete), "Archived projects should allow Delete");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Load), "Archived projects should NOT allow Load");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Generate), "Archived projects should NOT allow Generate");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Archive), "Archived projects should NOT allow Archive");
    }

    [Fact]
    public async Task ListProjectsAsync_CalculatesAvailableActions_ForUnexistingStatus()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        await store.CreateProjectAsync(new ProjectDto("UnexistingProject", FakeProjectTypeAName, ProjectStatus.Unexisting, DateTime.UtcNow, null));
        var manager = new ProjectsManager(store);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        var project = projects[0];
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Delete), "Unexisting projects should allow Delete");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Load), "Unexisting projects should NOT allow Load");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Generate), "Unexisting projects should NOT allow Generate");
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Archive), "Unexisting projects should NOT allow Archive");
    }

    [Fact]
    public async Task CreateProjectAsync_SetsAvailableActionsForNewProject()
    {
        // Arrange
        TextureProjectRegistry.RegisterType<FakeProjectTypeA>();
        var store = new FakeProjectsStore();
        var manager = new ProjectsManager(store);

        // Act
        await manager.CreateProjectAsync("TestProject", FakeProjectTypeAName);
        var projects = await manager.ListProjectsAsync();

        // Assert
        var project = projects[0];
        Assert.Equal(ProjectStatus.New, project.Status);
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Load));
        Assert.True(project.AvailableActions.HasFlag(ProjectActions.Delete));
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Generate));
        Assert.False(project.AvailableActions.HasFlag(ProjectActions.Archive));
    }
}
