using TileTextureGenerator.Adapters.UseCases.Dto;

namespace TileTextureGenerator.Adapters.UseCases.Ports.Input;

public interface IProjectSelectionUseCase
{
    Task<TextureProjectDto> CreateAsync(TextureProjectDto Project);
    Task<TextureProjectDto> OpenAsync(TextureProjectDto Project);
    Task<TextureProjectDto> ArchiveAsync(TextureProjectDto Project);
    Task<bool> DeleteAsync(TextureProjectDto Project);
    Task<bool> SaveAsync(TextureProjectDto project);
    Task<IReadOnlyList<string>> GetProjectTypeListAsync();
    List<string> GetProjectStatusList();
    Task<IReadOnlyList<TextureProjectDto>> GetProjectListAsync();
}
