using MyApi.Data;
using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Projects.Services
{
    public class TaskCommentService : ITaskCommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskCommentService> _logger;

        public TaskCommentService(ApplicationDbContext context, ILogger<TaskCommentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskCommentListResponseDto> GetTaskCommentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskComments.Where(c => !c.IsDeleted);

                if (projectTaskId.HasValue)
                    query = query.Where(c => c.ProjectTaskId == projectTaskId.Value);

                if (dailyTaskId.HasValue)
                    query = query.Where(c => c.DailyTaskId == dailyTaskId.Value);

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var comments = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var commentDtos = comments.Select(MapToCommentDto).ToList();

                return new TaskCommentListResponseDto
                {
                    Comments = commentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task comments");
                throw;
            }
        }

        public async Task<TaskCommentResponseDto?> GetCommentByIdAsync(int id)
        {
            try
            {
                var comment = await _context.TaskComments
                    .Include(c => c.ProjectTask)
                    .Include(c => c.DailyTask)
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                return comment != null ? MapToCommentDto(comment) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment by id {CommentId}", id);
                throw;
            }
        }

        public async Task<TaskCommentResponseDto> CreateCommentAsync(CreateTaskCommentRequestDto createDto, string createdByUser)
        {
            try
            {
                var comment = new TaskComment
                {
                    ProjectTaskId = createDto.ProjectTaskId,
                    DailyTaskId = createDto.DailyTaskId,
                    Content = createDto.Content,
                    AuthorId = createDto.AuthorId,
                    AuthorName = createDto.AuthorName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TaskComments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comment created successfully with ID {CommentId}", comment.Id);
                return MapToCommentDto(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                throw;
            }
        }

        public async Task<TaskCommentResponseDto?> UpdateCommentAsync(int id, UpdateTaskCommentRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var comment = await _context.TaskComments
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (comment == null)
                    return null;

                comment.Content = updateDto.Content;
                comment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Comment updated successfully with ID {CommentId}", id);
                return MapToCommentDto(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment with ID {CommentId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(int id, string deletedByUser)
        {
            try
            {
                var comment = await _context.TaskComments
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (comment == null)
                    return false;

                comment.IsDeleted = true;
                comment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Comment deleted successfully with ID {CommentId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment with ID {CommentId}", id);
                throw;
            }
        }

        public async Task<TaskCommentListResponseDto> SearchCommentsAsync(TaskCommentSearchRequestDto searchRequest)
        {
            return await GetTaskCommentsAsync(searchRequest.ProjectTaskId, searchRequest.DailyTaskId, searchRequest.PageNumber, searchRequest.PageSize);
        }

        public async Task<TaskCommentListResponseDto> GetCommentsByAuthorAsync(int authorId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskComments
                    .Where(c => c.AuthorId == authorId && !c.IsDeleted);

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var comments = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var commentDtos = comments.Select(MapToCommentDto).ToList();

                return new TaskCommentListResponseDto
                {
                    Comments = commentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments by author {AuthorId}", authorId);
                throw;
            }
        }

        public async Task<TaskCommentListResponseDto> GetRecentCommentsAsync(int? projectId = null, int? userId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskComments
                    .Include(c => c.ProjectTask)
                    .Where(c => !c.IsDeleted);

                if (projectId.HasValue)
                    query = query.Where(c => c.ProjectTask != null && c.ProjectTask.ProjectId == projectId.Value);

                if (userId.HasValue)
                    query = query.Where(c => c.AuthorId == userId.Value);

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var comments = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var commentDtos = comments.Select(MapToCommentDto).ToList();

                return new TaskCommentListResponseDto
                {
                    Comments = commentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent comments");
                throw;
            }
        }

        public async Task<bool> CommentExistsAsync(int id)
        {
            return await _context.TaskComments.AnyAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<bool> UserCanAccessCommentAsync(int commentId, int userId)
        {
            // Basic implementation - can be enhanced with proper authorization
            return true;
        }

        public async Task<bool> UserCanEditCommentAsync(int commentId, int userId)
        {
            var comment = await _context.TaskComments
                .Where(c => c.Id == commentId && !c.IsDeleted)
                .FirstOrDefaultAsync();

            return comment?.AuthorId == userId;
        }

        public async Task<bool> UserCanDeleteCommentAsync(int commentId, int userId)
        {
            return await UserCanEditCommentAsync(commentId, userId);
        }

        public async Task<int> GetTaskCommentCountAsync(int? projectTaskId = null, int? dailyTaskId = null)
        {
            var query = _context.TaskComments.Where(c => !c.IsDeleted);

            if (projectTaskId.HasValue)
                query = query.Where(c => c.ProjectTaskId == projectTaskId.Value);

            if (dailyTaskId.HasValue)
                query = query.Where(c => c.DailyTaskId == dailyTaskId.Value);

            return await query.CountAsync();
        }

        public async Task<int> GetUserCommentCountAsync(int userId, DateTime? fromDate = null)
        {
            var query = _context.TaskComments
                .Where(c => c.AuthorId == userId && !c.IsDeleted);

            if (fromDate.HasValue)
                query = query.Where(c => c.CreatedAt >= fromDate.Value);

            return await query.CountAsync();
        }

        public async Task<Dictionary<int, int>> GetTaskCommentCountsAsync(List<int> taskIds, bool isProjectTasks = true)
        {
            var query = _context.TaskComments.Where(c => !c.IsDeleted);

            if (isProjectTasks)
                query = query.Where(c => c.ProjectTaskId.HasValue && taskIds.Contains(c.ProjectTaskId.Value));
            else
                query = query.Where(c => c.DailyTaskId.HasValue && taskIds.Contains(c.DailyTaskId.Value));

            return await query
                .GroupBy(c => isProjectTasks ? c.ProjectTaskId!.Value : c.DailyTaskId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<List<TaskCommentResponseDto>> GetMostRecentCommentsAsync(int count = 10)
        {
            var comments = await _context.TaskComments
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();

            return comments.Select(MapToCommentDto).ToList();
        }

        public async Task<bool> BulkDeleteCommentsAsync(List<int> commentIds, string deletedByUser)
        {
            try
            {
                var comments = await _context.TaskComments
                    .Where(c => commentIds.Contains(c.Id) && !c.IsDeleted)
                    .ToListAsync();

                foreach (var comment in comments)
                {
                    comment.IsDeleted = true;
                    comment.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk deleted {Count} comments", comments.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk comment deletion");
                throw;
            }
        }

        public async Task<bool> DeleteAllTaskCommentsAsync(string deletedByUser, int? projectTaskId = null, int? dailyTaskId = null)
        {
            try
            {
                var query = _context.TaskComments.Where(c => !c.IsDeleted);

                if (projectTaskId.HasValue)
                    query = query.Where(c => c.ProjectTaskId == projectTaskId.Value);

                if (dailyTaskId.HasValue)
                    query = query.Where(c => c.DailyTaskId == dailyTaskId.Value);

                var comments = await query.ToListAsync();

                foreach (var comment in comments)
                {
                    comment.IsDeleted = true;
                    comment.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted all comments for task");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all task comments");
                throw;
            }
        }

        private TaskCommentResponseDto MapToCommentDto(TaskComment comment)
        {
            return new TaskCommentResponseDto
            {
                Id = comment.Id,
                ProjectTaskId = comment.ProjectTaskId,
                DailyTaskId = comment.DailyTaskId,
                TaskTitle = comment.ProjectTask?.Title ?? comment.DailyTask?.Title ?? string.Empty,
                Content = comment.Content,
                AuthorId = comment.AuthorId,
                AuthorName = comment.AuthorName,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsEdited = comment.UpdatedAt > comment.CreatedAt.AddMinutes(1)
            };
        }
    }
}