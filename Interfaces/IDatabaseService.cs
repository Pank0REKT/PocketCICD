using PocketCICD.Models;

namespace PocketCICD.Interfaces;

public interface IDatabaseService
{
    Task<IEnumerable<ProjectPathsModel>> GetAllProjectsAsync();
    Task<ProjectPathsModel?> GetProjectByIdAsync(int projectId);
    Task<int> SaveProjectAsync(ProjectPathsModel project);
    Task DeleteProjectAsync(int projectId);
}