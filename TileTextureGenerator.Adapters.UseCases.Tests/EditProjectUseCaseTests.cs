using SkiaSharp;
using TileTextureGenerator.Adapters.UseCases;
using TileTextureGenerator.Core.DTOs;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Tests.Common;
using Xunit;

namespace TileTextureGenerator.Adapters.UseCases.Tests;

public class EditProjectUseCaseTests
{
    [Fact]
    public void Constructor_WithNullProject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EditProjectUseCase(null!));
    }

    [Fact]
    public void Constructor_WithValidProject_SetsProject()
    {
        // Arrange
        var project = CreateMockProject("TestProject");

        // Act
        var useCase = new EditProjectUseCase(project);

        // Assert
        Assert.NotNull(useCase.Project);
        Assert.Equal("TestProject", useCase.Project.Name);
    }

    [Fact]
    public void GetProjectType_ReturnsProjectType()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act
        var type = useCase.GetProjectType();

        // Assert
        Assert.Equal("MockProject", type);
    }

    [Fact]
    public async Task GetAvailableTransformationTypesAsync_ReturnsTransformedList()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act
        var types = await useCase.GetAvailableTransformationTypesAsync();

        // Assert
        Assert.NotNull(types);
        Assert.Equal(2, types.Count);
        Assert.Equal("HorizontalFloor", types[0].TechnicalName);
        Assert.NotEmpty(types[0].Icon);
        Assert.Equal("VerticalWall", types[1].TechnicalName);
        Assert.Empty(types[1].Icon); // Null Icon returns empty array
    }

    [Fact]
    public async Task SaveAsync_CallsProjectSaveChangesAsync()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act
        await useCase.SaveAsync();

        // Assert
        Assert.True(((MockProject)project).SaveChangesCalled);
    }

    [Fact]
    public async Task AddTransformationAsync_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.AddTransformationAsync(null!));
    }

    [Fact]
    public async Task AddTransformationAsync_WithEmptyType_ThrowsArgumentException()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.AddTransformationAsync(""));
    }

    [Fact]
    public async Task AddTransformationAsync_WithWhitespaceType_ThrowsArgumentException()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.AddTransformationAsync("   "));
    }

    [Fact]
    public async Task AddTransformationAsync_WithValidType_CallsProjectAddTransformation()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act
        await useCase.AddTransformationAsync("HorizontalFloor");

        // Assert
        Assert.True(((MockProject)project).AddTransformationCalled);
        Assert.Equal("HorizontalFloor", ((MockProject)project).AddedTransformationType);
    }

    [Fact]
    public async Task RemoveTransformationAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.RemoveTransformationAsync(Guid.Empty));
    }

    [Fact]
    public async Task RemoveTransformationAsync_WithValidGuid_CallsProjectRemoveTransformation()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);
        var transformationId = Guid.NewGuid();

        // Add a transformation first so it can be removed
        project.Transformations.Add(new TransformationDTO { Id = transformationId, Type = "TestTransform", Icon = null });

        // Act
        await useCase.RemoveTransformationAsync(transformationId);

        // Assert
        Assert.True(((MockProject)project).RemoveTransformationCalled);
        Assert.Equal(transformationId, ((MockProject)project).RemovedTransformationId);
    }

    [Fact]
    public async Task GetTransformationAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.GetTransformationAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetTransformationAsync_WithValidGuid_ReturnsTransformation()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);
        var transformationId = Guid.NewGuid();

        // Act
        var transformation = await useCase.GetTransformationAsync(transformationId);

        // Assert
        Assert.NotNull(transformation);
        Assert.True(((MockProject)project).GetTransformationCalled);
    }

    [Fact]
    public async Task GenerateAsync_CallsProjectGenerateAsync()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act
        var result = await useCase.GenerateAsync();

        // Assert
        Assert.True(result);
        // Note: ProjectBase.GenerateAsync() is not virtual and always returns non-null Task<bool>
        // so we can only verify the result
    }

    [Fact]
    public async Task ArchiveAsync_CallsProjectArchiveAsync()
    {
        // Arrange
        var project = CreateMockProject("TestProject");
        var useCase = new EditProjectUseCase(project);

        // Act
        var result = await useCase.ArchiveAsync();

        // Assert
        Assert.True(result);
        // Note: Cannot verify ArchiveCalled because ProjectBase.ArchiveAsync is not virtual
        // We can only verify the result
    }

    #region Helper Methods

    private static MockProject CreateMockProject(string name)
    {
        var project = new MockProject();
        project.Initialize(name);
        return project;
    }

    #endregion

    #region Mock Classes

    private class MockProject : ProjectBase
    {
        public bool SaveChangesCalled { get; private set; }
        public bool AddTransformationCalled { get; private set; }
        public string? AddedTransformationType { get; private set; }
        public bool RemoveTransformationCalled { get; private set; }
        public Guid RemovedTransformationId { get; private set; }
        public bool GetTransformationCalled { get; private set; }

        public MockProject() : base(new MockProjectStore())
        {
        }

        public override async Task SaveChangesAsync()
        {
            SaveChangesCalled = true;
            await base.SaveChangesAsync();
        }

        public override async Task AddTransformationAsync(string transformationType)
        {
            AddTransformationCalled = true;
            AddedTransformationType = transformationType;
            await base.AddTransformationAsync(transformationType);
        }

        public override async Task RemoveTransformationAsync(Guid transformationId)
        {
            RemoveTransformationCalled = true;
            RemovedTransformationId = transformationId;
            await base.RemoveTransformationAsync(transformationId);
        }

        public override async Task<TransformationBase> GetTransformationAsync(Guid transformationId)
        {
            GetTransformationCalled = true;
            return await Task.FromResult(new MockTransformation());
        }

        public override Task<IReadOnlyList<TransformationTypeDTO>> GetAvailableTransformationTypesAsync()
        {
            var types = new List<TransformationTypeDTO>
            {
                new() { Name = "HorizontalFloor", Icon = TestImageFactory.CreateImageData() },
                new() { Name = "VerticalWall", Icon = null }
            };
            return Task.FromResult<IReadOnlyList<TransformationTypeDTO>>(types);
        }
    }

    private class MockProjectStore : IProjectStore
    {
        public Task SaveAsync(ProjectBase project) => Task.CompletedTask;
        public Task AddTransformationAsync(ProjectBase project, TransformationDTO transformation) => Task.CompletedTask;
        public Task RemoveTransformationAsync(ProjectBase project, Guid transformationId) => Task.CompletedTask;
        public Task<TransformationBase?> LoadTransformationAsync(ProjectBase project, Guid transformationId) => 
            Task.FromResult<TransformationBase?>(new MockTransformation());
        public Task ArchiveAsync(ProjectBase project) => Task.CompletedTask;
    }

    private class MockTransformation : TransformationBase
    {
        public MockTransformation() : base(new MockTransformationStore())
        {
        }

        protected override Task<ImageData> ExecuteAsync()
        {
            return Task.FromResult(new ImageData([4, 5, 6]));
        }
    }

    private class MockTransformationStore : Core.Ports.Output.ITransformationStore
    {
        public Task SaveAsync(TransformationBase transformation) => Task.CompletedTask;
    }

    #endregion
}
