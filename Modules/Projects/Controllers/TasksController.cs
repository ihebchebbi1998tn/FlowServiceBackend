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
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        #region Project Tasks

        /// <summary>
        /// Get all tasks for a project
        /// </summary>
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<ProjectTaskResponseDto>>> GetProjectTasks(int projectId)
        {
            try
            {
                var tasks = await _taskService.GetProjectTasksAsync(projectId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for project {ProjectId}", projectId);
                return StatusCode(500, "An error occurred while retrieving project tasks");
            }
        }

        /// <summary>
        /// Get all tasks for a column
        /// </summary>
        [HttpGet("column/{columnId}")]
        public async Task<ActionResult<List<ProjectTaskResponseDto>>> GetColumnTasks(int columnId)
        {
            try
            {
                var tasks = await _taskService.GetColumnTasksAsync(columnId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for column {ColumnId}", columnId);
                return StatusCode(500, "An error occurred while retrieving column tasks");
            }
        }

        /// <summary>
        /// Get project task by ID
        /// </summary>
        [HttpGet("project-task/{id}")]
        public async Task<ActionResult<ProjectTaskResponseDto>> GetProjectTask(int id)
        {
            try
            {
                var task = await _taskService.GetProjectTaskByIdAsync(id);
                
                if (task == null)
                {
                    return NotFound($"Project task with ID {id} not found");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the project task");
            }
        }

        /// <summary>
        /// Create a new project task
        /// </summary>
        [HttpPost("project-task")]
        public async Task<ActionResult<ProjectTaskResponseDto>> CreateProjectTask([FromBody] CreateProjectTaskRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var task = await _taskService.CreateProjectTaskAsync(createDto, currentUser);

                return CreatedAtAction(nameof(GetProjectTask), new { id = task.Id }, task);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating project task");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project task");
                return StatusCode(500, "An error occurred while creating the project task");
            }
        }

        /// <summary>
        /// Update an existing project task
        /// </summary>
        [HttpPut("project-task/{id}")]
        public async Task<ActionResult<ProjectTaskResponseDto>> UpdateProjectTask(int id, [FromBody] UpdateProjectTaskRequestDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var task = await _taskService.UpdateProjectTaskAsync(id, updateDto, currentUser);

                if (task == null)
                {
                    return NotFound($"Project task with ID {id} not found");
                }

                return Ok(task);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating project task with ID {TaskId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the project task");
            }
        }

        /// <summary>
        /// Delete a project task (soft delete)
        /// </summary>
        [HttpDelete("project-task/{id}")]
        public async Task<ActionResult> DeleteProjectTask(int id)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _taskService.DeleteProjectTaskAsync(id, currentUser);

                if (!success)
                {
                    return NotFound($"Project task with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while deleting the project task");
            }
        }

        #endregion

        #region Daily Tasks

        /// <summary>
        /// Get all daily tasks for a user
        /// </summary>
        [HttpGet("daily/user/{userId}")]
        public async Task<ActionResult<List<DailyTaskResponseDto>>> GetUserDailyTasks(int userId)
        {
            try
            {
                var tasks = await _taskService.GetUserDailyTasksAsync(userId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily tasks for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving daily tasks");
            }
        }

        /// <summary>
        /// Get daily task by ID
        /// </summary>
        [HttpGet("daily-task/{id}")]
        public async Task<ActionResult<DailyTaskResponseDto>> GetDailyTask(int id)
        {
            try
            {
                var task = await _taskService.GetDailyTaskByIdAsync(id);
                
                if (task == null)
                {
                    return NotFound($"Daily task with ID {id} not found");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the daily task");
            }
        }

        /// <summary>
        /// Create a new daily task
        /// </summary>
        [HttpPost("daily-task")]
        public async Task<ActionResult<DailyTaskResponseDto>> CreateDailyTask([FromBody] CreateDailyTaskRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var task = await _taskService.CreateDailyTaskAsync(createDto, currentUser);

                return CreatedAtAction(nameof(GetDailyTask), new { id = task.Id }, task);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating daily task");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating daily task");
                return StatusCode(500, "An error occurred while creating the daily task");
            }
        }

        /// <summary>
        /// Update an existing daily task
        /// </summary>
        [HttpPut("daily-task/{id}")]
        public async Task<ActionResult<DailyTaskResponseDto>> UpdateDailyTask(int id, [FromBody] UpdateDailyTaskRequestDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var task = await _taskService.UpdateDailyTaskAsync(id, updateDto, currentUser);

                if (task == null)
                {
                    return NotFound($"Daily task with ID {id} not found");
                }

                return Ok(task);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating daily task with ID {TaskId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating daily task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the daily task");
            }
        }

        /// <summary>
        /// Delete a daily task (soft delete)
        /// </summary>
        [HttpDelete("daily-task/{id}")]
        public async Task<ActionResult> DeleteDailyTask(int id)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _taskService.DeleteDailyTaskAsync(id, currentUser);

                if (!success)
                {
                    return NotFound($"Daily task with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting daily task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while deleting the daily task");
            }
        }

        #endregion

        #region Task Search and Filtering

        /// <summary>
        /// Search tasks with filters
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<TaskListResponseDto>> SearchTasks([FromQuery] TaskSearchRequestDto searchRequest)
        {
            try
            {
                var result = await _taskService.SearchTasksAsync(searchRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching tasks");
                return StatusCode(500, "An error occurred while searching tasks");
            }
        }

        /// <summary>
        /// Get tasks by assignee
        /// </summary>
        [HttpGet("assignee/{assigneeId}")]
        public async Task<ActionResult<List<ProjectTaskResponseDto>>> GetTasksByAssignee(int assigneeId, [FromQuery] int? projectId = null)
        {
            try
            {
                var tasks = await _taskService.GetTasksByAssigneeAsync(assigneeId, projectId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by assignee {AssigneeId}", assigneeId);
                return StatusCode(500, "An error occurred while retrieving tasks");
            }
        }

        /// <summary>
        /// Get overdue tasks
        /// </summary>
        [HttpGet("overdue")]
        public async Task<ActionResult<List<ProjectTaskResponseDto>>> GetOverdueTasks([FromQuery] int? projectId = null, [FromQuery] int? assigneeId = null)
        {
            try
            {
                var tasks = await _taskService.GetOverdueTasksAsync(projectId, assigneeId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks");
                return StatusCode(500, "An error occurred while retrieving overdue tasks");
            }
        }

        /// <summary>
        /// Get tasks by contact
        /// </summary>
        [HttpGet("contact/{contactId}")]
        public async Task<ActionResult<List<ProjectTaskResponseDto>>> GetTasksByContact(int contactId)
        {
            try
            {
                var tasks = await _taskService.GetTasksByContactAsync(contactId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by contact {ContactId}", contactId);
                return StatusCode(500, "An error occurred while retrieving tasks");
            }
        }

        #endregion

        #region Task Movement and Positioning

        /// <summary>
        /// Move a task to a different column/position
        /// </summary>
        [HttpPut("{taskId}/move")]
        public async Task<ActionResult> MoveTask(int taskId, [FromBody] MoveTaskRequestDto moveDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var success = await _taskService.MoveTaskAsync(taskId, moveDto, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to move task");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while moving the task");
            }
        }

        /// <summary>
        /// Bulk move multiple tasks
        /// </summary>
        [HttpPut("bulk/move")]
        public async Task<ActionResult> BulkMoveTasks([FromBody] BulkMoveTasksRequestDto bulkMoveDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var success = await _taskService.BulkMoveTasksAsync(bulkMoveDto, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to move tasks");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk task move");
                return StatusCode(500, "An error occurred during bulk task move");
            }
        }

        #endregion

        #region Task Assignment

        /// <summary>
        /// Assign a task to a user
        /// </summary>
        [HttpPut("{taskId}/assign")]
        public async Task<ActionResult> AssignTask(int taskId, [FromBody] AssignTaskRequestDto assignDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var success = await _taskService.AssignTaskAsync(taskId, assignDto, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to assign task");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while assigning the task");
            }
        }

        /// <summary>
        /// Unassign a task
        /// </summary>
        [HttpPut("{taskId}/unassign")]
        public async Task<ActionResult> UnassignTask(int taskId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _taskService.UnassignTaskAsync(taskId, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to unassign task");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while unassigning the task");
            }
        }

        /// <summary>
        /// Bulk assign multiple tasks
        /// </summary>
        [HttpPut("bulk/assign")]
        public async Task<ActionResult> BulkAssignTasks([FromBody] BulkAssignTasksRequestDto bulkAssignDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var success = await _taskService.BulkAssignTasksAsync(bulkAssignDto, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to assign tasks");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk task assignment");
                return StatusCode(500, "An error occurred during bulk task assignment");
            }
        }

        #endregion

        #region Task Status Management

        /// <summary>
        /// Update task status
        /// </summary>
        [HttpPut("{taskId}/status")]
        public async Task<ActionResult> UpdateTaskStatus(int taskId, [FromBody] string status)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _taskService.UpdateTaskStatusAsync(taskId, status, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to update task status");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while updating task status");
            }
        }

        /// <summary>
        /// Complete a task
        /// </summary>
        [HttpPut("{taskId}/complete")]
        public async Task<ActionResult> CompleteTask(int taskId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _taskService.CompleteTaskAsync(taskId, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to complete task");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while completing the task");
            }
        }

        /// <summary>
        /// Bulk update task status
        /// </summary>
        [HttpPut("bulk/status")]
        public async Task<ActionResult> BulkUpdateTaskStatus([FromBody] BulkUpdateTaskStatusDto bulkUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var success = await _taskService.BulkUpdateTaskStatusAsync(bulkUpdateDto, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to update task statuses");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk task status update");
                return StatusCode(500, "An error occurred during bulk status update");
            }
        }

        #endregion

        private string GetCurrentUser()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? 
                   User.FindFirst(ClaimTypes.Name)?.Value ?? 
                   User.FindFirst("email")?.Value ?? 
                   "system";
        }
    }
}
