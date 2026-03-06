using api.data;
using api.dtos.Project;
using api.interfaces;
using api.mappers;
using Microsoft.AspNetCore.Mvc;

namespace api.controller
{
    [Route("api/project")]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IProjectRepository _projectRepo;

        public ProjectController(ApplicationDBContext context, IProjectRepository projectRepo)
        {
            _projectRepo = projectRepo;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(){
            var projects = await _projectRepo.GetAllAsync();
            var projectDto = projects.Select(p => p.ToProjectDto());

            return Ok(projects);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id) 
        {
            var project = await _projectRepo.GetByIdAsync(id);

            if (project == null) { 
                return NotFound(); 
            }
            return Ok(project.ToProjectDto());
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateProjectRequestDto updateDto) 
        {
            var projectModel = await _projectRepo.UpdateAsync(id, updateDto);

            if (projectModel == null) {
                return NotFound();
            }

            return Ok(projectModel.ToProjectDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectRequesDto projectDto) 
        {
            var projectModel = projectDto.ToProjectFromCreateDTO();
            await _projectRepo.CreateSync(projectModel);
            return CreatedAtAction(nameof(GetById), new {id = projectModel.Id}, projectModel.ToProjectDto());
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id) 
        {
            var projectModel = await _projectRepo.DeleteAsync(id);

            if (projectModel == null) {
                return NotFound();
            }

            return NoContent();
        }
    }
}