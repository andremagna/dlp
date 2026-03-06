using api.controller.dtos.Comment.Project;
using api.dtos.Project;
using api.models;

namespace api.mappers
{
    public static class ProjectMappers
    {
        public static ProjectDto ToProjectDto(this Project projectModel){
            return new ProjectDto {
                Id = projectModel.Id,
                ProjectName = projectModel.ProjectName,
                ProjectDescription = projectModel.ProjectDescription,
                ProjectPlace = projectModel.ProjectPlace,
                ProjectYear = projectModel.ProjectYear,
                ProjectParentFilter = projectModel.ProjectParentFilter,
                ProjectChildFilter = projectModel.ProjectChildFilter,
                ProjectCover = projectModel.ProjectCover,
                ProjectImages = projectModel.ProjectImages,
            };
        }

        public static Project ToProjectFromCreateDTO(this CreateProjectRequesDto projectDto){
            return new Project {
                ProjectName = projectDto.ProjectName,
                ProjectDescription = projectDto.ProjectDescription,
                ProjectPlace = projectDto.ProjectPlace,
                ProjectYear = projectDto.ProjectYear,
                ProjectParentFilter = projectDto.ProjectParentFilter,
                ProjectChildFilter = projectDto.ProjectChildFilter,
                ProjectCover = projectDto.ProjectCover,
                ProjectImages = projectDto.ProjectImages,
            };
        }
    }
}