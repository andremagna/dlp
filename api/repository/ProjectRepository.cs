using api.data;
using api.dtos.Project;
using api.interfaces;
using api.mappers;
using api.models;
using Microsoft.EntityFrameworkCore;

namespace api.repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDBContext _context;
    
        public ProjectRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<Project?> CreateSync(Project projectModel)
        {
            await _context.Projects.AddAsync(projectModel);
            await _context.SaveChangesAsync();
            return projectModel;
        }

        public async Task<Project?> DeleteAsync(int id)
        {
            var projectModel = await _context.Projects.FirstOrDefaultAsync(x => x.Id == id);

            if (projectModel == null) {
                return null;
            }

            _context.Projects.Remove(projectModel);
            await _context.SaveChangesAsync();

            return projectModel;
        }

        public async Task<List<Project>> GetAllAsync()
        {
            return await _context.Projects.ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            return await _context.Projects.FindAsync(id);
        }

        public async Task<Project?> UpdateAsync(int id, UpdateProjectRequestDto projectDto)
        {
            var existingProject = await _context.Projects.FirstOrDefaultAsync(x => x.Id == id);

            if (existingProject == null) {
                return null;
            }

            existingProject.ProjectName = projectDto.ProjectName;
            existingProject.ProjectDescription = projectDto.ProjectDescription;
            existingProject.ProjectPlace = projectDto.ProjectPlace;
            existingProject.ProjectYear = projectDto.ProjectYear;
            existingProject.ProjectParentFilter = projectDto.ProjectParentFilter;
            existingProject.ProjectChildFilter = projectDto.ProjectChildFilter;
            existingProject.ProjectCover = projectDto.ProjectCover;
            existingProject.ProjectImages = projectDto.ProjectImages;

            await _context.SaveChangesAsync();

            return existingProject;
        }
    }
}