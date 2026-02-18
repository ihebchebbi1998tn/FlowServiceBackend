using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Services;
using MyApi.Modules.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MyApi.Modules.Projects.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ISystemLogService _systemLogService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IProjectService projectService, 
            ISystemLogService systemLogService,
            ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _systemLogService = systemLogService;
            _logger = logger;
        }

        /// <summary>
        /// Get all projects with optional filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ProjectListResponseDto>> GetAllProjects([FromQuery] ProjectSearchRequestDto? searchRequest = null)
        {
            try
            {
                var result = await _projectService.GetAllProjectsAsync(searchRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all projects");
                await _systemLogService.LogErrorAsync("Failed to retrieve projects", "Projects", "read", GetCurrentUserId(), GetCurrentUser(), details: ex.Message);
                return StatusCode(500, "An error occurred while retrieving projects");
            }
        }

        /// <summary>
        /// Get project by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> GetProject(int id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                
                if (project == null)
                {
                    return NotFound($"Project with ID {id} not found");
                }

                return Ok(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project with ID {ProjectId}", id);
                await _systemLogService.LogErrorAsync($"Failed to retrieve project {id}", "Projects", "read", GetCurrentUserId(), GetCurrentUser(), "Project", id.ToString(), ex.Message);
                return StatusCode(500, "An error occurred while retrieving the project");
            }
        }

        /// <summary>
        /// Create a new project
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> CreateProject([FromBody] CreateProjectRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var project = await _projectService.CreateProjectAsync(createDto, currentUser);

                await _systemLogService.LogSuccessAsync($"Project created: {createDto.Name}", "Projects", "create", GetCurrentUserId(), currentUser, "Project", project.Id.ToString());

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating project");
                await _systemLogService.LogWarningAsync($"Failed to create project: {ex.Message}", "Projects", "create", GetCurrentUserId(), GetCurrentUser(), "Project", details: ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                await _systemLogService.LogErrorAsync("Failed to create project", "Projects", "create", GetCurrentUserId(), GetCurrentUser(), "Project", details: ex.Message);
                return StatusCode(500, "An error occurred while creating the project");
            }
        }

        /// <summary>
        /// Update an existing project
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> UpdateProject(int id, [FromBody] UpdateProjectRequestDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var project = await _projectService.UpdateProjectAsync(id, updateDto, currentUser);

                if (project == null)
                {
                    return NotFound($"Project with ID {id} not found");
                }

                await _systemLogService.LogSuccessAsync($"Project updated: {project.Name}", "Projects", "update", GetCurrentUserId(), currentUser, "Project", id.ToString());

                return Ok(project);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating project with ID {ProjectId}", id);
                await _systemLogService.LogWarningAsync($"Failed to update project {id}: {ex.Message}", "Projects", "update", GetCurrentUserId(), GetCurrentUser(), "Project", id.ToString(), ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project with ID {ProjectId}", id);
                await _systemLogService.LogErrorAsync($"Failed to update project {id}", "Projects", "update", GetCurrentUserId(), GetCurrentUser(), "Project", id.ToString(), ex.Message);
                return StatusCode(500, "An error occurred while updating the project");
            }
        }

        /// <summary>
        /// Delete a project
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProject(int id)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _projectService.DeleteProjectAsync(id, currentUser);

                if (!success)
                {
                    return NotFound($"Project with ID {id} not found");
                }

                await _systemLogService.LogSuccessAsync($"Project deleted: ID {id}", "Projects", "delete", GetCurrentUserId(), currentUser, "Project", id.ToString());

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project with ID {ProjectId}", id);
                await _systemLogService.LogErrorAsync($"Failed to delete project {id}", "Projects", "delete", GetCurrentUserId(), GetCurrentUser(), "Project", id.ToString(), ex.Message);
                return StatusCode(500, "An error occurred while deleting the project");
            }
        }

        /// <summary>
        /// Search projects
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<ProjectResponseDto>>> SearchProjects([FromQuery] string searchTerm)
        {
            try
            {
                var result = await _projectService.SearchProjectsAsync(searchTerm ?? "");
                return Ok(new { projects = result, totalCount = result.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching projects with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching projects");
            }
        }

        /// <summary>
        /// Get project statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<ProjectStatisticsDto>> GetStatistics()
        {
            try
            {
                var stats = await _projectService.GetStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project statistics");
                return StatusCode(500, "An error occurred while retrieving project statistics");
            }
        }

        private string GetCurrentUser()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? 
                   User.FindFirst(ClaimTypes.Name)?.Value ?? 
                   User.FindFirst("email")?.Value ?? 
                   "system";
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
