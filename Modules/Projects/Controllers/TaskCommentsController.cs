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
    public class TaskCommentsController : ControllerBase
    {
        private readonly ITaskCommentService _commentService;
        private readonly ILogger<TaskCommentsController> _logger;

        public TaskCommentsController(ITaskCommentService commentService, ILogger<TaskCommentsController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        /// <summary>
        /// Get comments for a project task
        /// </summary>
        [HttpGet("project-task/{projectTaskId}")]
        public async Task<ActionResult<TaskCommentListResponseDto>> GetProjectTaskComments(int projectTaskId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var comments = await _commentService.GetTaskCommentsAsync(projectTaskId: projectTaskId, pageNumber: pageNumber, pageSize: pageSize);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for project task {TaskId}", projectTaskId);
                return StatusCode(500, "An error occurred while retrieving comments");
            }
        }

        /// <summary>
        /// Get comments for a daily task
        /// </summary>
        [HttpGet("daily-task/{dailyTaskId}")]
        public async Task<ActionResult<TaskCommentListResponseDto>> GetDailyTaskComments(int dailyTaskId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var comments = await _commentService.GetTaskCommentsAsync(dailyTaskId: dailyTaskId, pageNumber: pageNumber, pageSize: pageSize);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for daily task {TaskId}", dailyTaskId);
                return StatusCode(500, "An error occurred while retrieving comments");
            }
        }

        /// <summary>
        /// Get comment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskCommentResponseDto>> GetComment(int id)
        {
            try
            {
                var comment = await _commentService.GetCommentByIdAsync(id);
                
                if (comment == null)
                {
                    return NotFound($"Comment with ID {id} not found");
                }

                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment with ID {CommentId}", id);
                return StatusCode(500, "An error occurred while retrieving the comment");
            }
        }

        /// <summary>
        /// Create a new comment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TaskCommentResponseDto>> CreateComment([FromBody] CreateTaskCommentRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var comment = await _commentService.CreateCommentAsync(createDto, currentUser);

                return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating comment");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, "An error occurred while creating the comment");
            }
        }

        /// <summary>
        /// Update an existing comment
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskCommentResponseDto>> UpdateComment(int id, [FromBody] UpdateTaskCommentRequestDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var comment = await _commentService.UpdateCommentAsync(id, updateDto, currentUser);

                if (comment == null)
                {
                    return NotFound($"Comment with ID {id} not found");
                }

                return Ok(comment);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to update comment {CommentId}", id);
                return Forbid("You can only edit your own comments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment with ID {CommentId}", id);
                return StatusCode(500, "An error occurred while updating the comment");
            }
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteComment(int id)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _commentService.DeleteCommentAsync(id, currentUser);

                if (!success)
                {
                    return NotFound($"Comment with ID {id} not found");
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to delete comment {CommentId}", id);
                return Forbid("You can only delete your own comments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment with ID {CommentId}", id);
                return StatusCode(500, "An error occurred while deleting the comment");
            }
        }

        /// <summary>
        /// Search comments
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<TaskCommentListResponseDto>> SearchComments([FromQuery] TaskCommentSearchRequestDto searchRequest)
        {
            try
            {
                var result = await _commentService.SearchCommentsAsync(searchRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching comments");
                return StatusCode(500, "An error occurred while searching comments");
            }
        }

        /// <summary>
        /// Get comments by author
        /// </summary>
        [HttpGet("author/{authorId}")]
        public async Task<ActionResult<TaskCommentListResponseDto>> GetCommentsByAuthor(int authorId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var comments = await _commentService.GetCommentsByAuthorAsync(authorId, pageNumber, pageSize);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments by author {AuthorId}", authorId);
                return StatusCode(500, "An error occurred while retrieving comments");
            }
        }

        /// <summary>
        /// Get recent comments
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<TaskCommentListResponseDto>> GetRecentComments([FromQuery] int? projectId = null, [FromQuery] int? userId = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var comments = await _commentService.GetRecentCommentsAsync(projectId, userId, pageNumber, pageSize);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent comments");
                return StatusCode(500, "An error occurred while retrieving recent comments");
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
