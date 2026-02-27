using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Core.Ports.Output
{
    public interface ITextureProjectStore
    {
        Task<TileTextureProjectBase> CreateNewProjectAsync(TileTextureProjectBase project);
        Task<TileTextureProjectBase> OpenProjectAsync(TileTextureProjectBase project);
        Task<TileTextureProjectBase> ArchiveProjectAsync(TileTextureProjectBase project);
        Task<bool> DeleteProjectAsync(string projectName);
        Task<IReadOnlyList<TileTextureProjectBase>> LoadProjectListAsync();
        Task<bool> SaveProjectAsync(TileTextureProjectBase project);

        /// <summary>
        /// Loads lightweight project summaries for list display (no full entity instantiation)
        /// </summary>
        Task<IReadOnlyList<TileTextureProjectSummary>> LoadProjectSummariesAsync();
    }
}
