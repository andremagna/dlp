using api.dtos.Project;
using api.models;

namespace api.interfaces
{
    public interface IProjectRepository
    {
        Task<List<Project?>> GetAllAsync();
        Task<Project?> GetByIdAsync(int id);
        Task<Project> CreateSync(Project projectModel);
        Task<Project?> UpdateAsync(int id, UpdateProjectRequestDto projectDto);
        Task<Project?> DeleteAsync(int id);
    }
}