using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Services;
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
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
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

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating project");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
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

                return Ok(project);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating project with ID {ProjectId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project with ID {ProjectId}", id);
                return StatusCode(500, "An error occurred while updating the project");
            }
        }

        /// <summary>
        /// Delete a project (soft delete)
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

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project with ID {ProjectId}", id);
                return StatusCode(500, "An error occurred while deleting the project");
            }
        }

        /// <summary>
        /// Search projects
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ProjectListResponseDto>> SearchProjects(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _projectService.SearchProjectsAsync(searchTerm, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching projects with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching projects");
            }
        }

        /// <summary>
        /// Get projects by owner
        /// </summary>
        [HttpGet("owner/{ownerId}")]
        public async Task<ActionResult<ProjectListResponseDto>> GetProjectsByOwner(int ownerId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _projectService.GetProjectsByOwnerAsync(ownerId, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects by owner {OwnerId}", ownerId);
                return StatusCode(500, "An error occurred while retrieving projects");
            }
        }

        /// <summary>
        /// Get projects by contact
        /// </summary>
        [HttpGet("contact/{contactId}")]
        public async Task<ActionResult<ProjectListResponseDto>> GetProjectsByContact(int contactId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _projectService.GetProjectsByContactAsync(contactId, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects by contact {ContactId}", contactId);
                return StatusCode(500, "An error occurred while retrieving projects");
            }
        }

        /// <summary>
        /// Get projects by team member
        /// </summary>
        [HttpGet("team-member/{userId}")]
        public async Task<ActionResult<ProjectListResponseDto>> GetProjectsByTeamMember(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _projectService.GetProjectsByTeamMemberAsync(userId, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects by team member {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving projects");
            }
        }

        /// <summary>
        /// Assign team member to project
        /// </summary>
        [HttpPost("{projectId}/team-members")]
        public async Task<ActionResult> AssignTeamMember(int projectId, [FromBody] AssignTeamMemberDto assignDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var success = await _projectService.AssignTeamMemberAsync(projectId, assignDto, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to assign team member to project");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning team member {UserId} to project {ProjectId}", assignDto.UserId, projectId);
                return StatusCode(500, "An error occurred while assigning the team member");
            }
        }

        /// <summary>
        /// Remove team member from project
        /// </summary>
        [HttpDelete("{projectId}/team-members")]
        public async Task<ActionResult> RemoveTeamMember(int projectId, [FromBody] RemoveTeamMemberDto removeDto)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _projectService.RemoveTeamMemberAsync(projectId, removeDto, currentUser);

                if (!success)
                {
                    return NotFound("Team member assignment not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing team member {UserId} from project {ProjectId}", removeDto.UserId, projectId);
                return StatusCode(500, "An error occurred while removing the team member");
            }
        }

        /// <summary>
        /// Get project team members
        /// </summary>
        [HttpGet("{projectId}/team-members")]
        public async Task<ActionResult<List<int>>> GetProjectTeamMembers(int projectId)
        {
            try
            {
                var teamMembers = await _projectService.GetProjectTeamMembersAsync(projectId);
                return Ok(teamMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members for project {ProjectId}", projectId);
                return StatusCode(500, "An error occurred while retrieving team members");
            }
        }

        /// <summary>
        /// Get project statistics
        /// </summary>
        [HttpGet("{projectId}/stats")]
        public async Task<ActionResult<ProjectStatsDto>> GetProjectStats(int projectId)
        {
            try
            {
                var stats = await _projectService.GetProjectStatsAsync(projectId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for project {ProjectId}", projectId);
                return StatusCode(500, "An error occurred while retrieving project statistics");
            }
        }

        /// <summary>
        /// Bulk update project status
        /// </summary>
        [HttpPut("bulk/status")]
        public async Task<ActionResult> BulkUpdateProjectStatus([FromBody] BulkUpdateProjectStatusDto bulkUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var success = await _projectService.BulkUpdateProjectStatusAsync(bulkUpdateDto, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to update project statuses");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk status update");
                return StatusCode(500, "An error occurred during bulk status update");
            }
        }

        /// <summary>
        /// Bulk archive/unarchive projects
        /// </summary>
        [HttpPut("bulk/archive")]
        public async Task<ActionResult> BulkArchiveProjects([FromBody] List<int> projectIds, [FromQuery] bool archive = true)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _projectService.BulkArchiveProjectsAsync(projectIds, archive, currentUser);

                if (!success)
                {
                    return BadRequest($"Failed to {(archive ? "archive" : "unarchive")} projects");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk archive operation");
                return StatusCode(500, "An error occurred during bulk archive operation");
            }
        }

        private string GetCurrentUser()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? 
                   User.FindFirst(ClaimTypes.Name)?.Value ?? 
                   User.FindFirst("email")?.Value ?? 
                   "system";
        }
    }
}
