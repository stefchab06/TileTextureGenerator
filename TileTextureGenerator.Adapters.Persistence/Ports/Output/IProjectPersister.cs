using TileTextureGenerator.Adapters.Persistence.Dto;

namespace TileTextureGenerator.Adapters.Persistence.Ports.Output;

public interface IProjectPersister
{
    public Task<Boolean> CreateProjectAsync(ProjectDataDto project);
    public Task<Boolean> DeleteProjectAsync(string projectName);
    public Task<ProjectDataDto> LoadProjectAsync(string projectName);
    public Task<Boolean> SaveProjectAsync(ProjectDataDto project);
    public Task<IList<ProjectDataDto>> GetProjectList();
}
