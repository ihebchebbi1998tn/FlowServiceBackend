using MyApi.Modules.Projects.DTOs;

namespace MyApi.Modules.Projects.Services
{
    public interface ITaskCommentService
    {
        // Comment CRUD operations
        Task<TaskCommentListResponseDto> GetTaskCommentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20);
        Task<TaskCommentResponseDto?> GetCommentByIdAsync(int id);
        Task<TaskCommentResponseDto> CreateCommentAsync(CreateTaskCommentRequestDto createDto, string createdByUser);
        Task<TaskCommentResponseDto?> UpdateCommentAsync(int id, UpdateTaskCommentRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteCommentAsync(int id, string deletedByUser);

        // Comment search and filtering
        Task<TaskCommentListResponseDto> SearchCommentsAsync(TaskCommentSearchRequestDto searchRequest);
        Task<TaskCommentListResponseDto> GetCommentsByAuthorAsync(int authorId, int pageNumber = 1, int pageSize = 20);
        Task<TaskCommentListResponseDto> GetRecentCommentsAsync(int? projectId = null, int? userId = null, int pageNumber = 1, int pageSize = 20);

        // Comment validation and utilities
        Task<bool> CommentExistsAsync(int id);
        Task<bool> UserCanAccessCommentAsync(int commentId, int userId);
        Task<bool> UserCanEditCommentAsync(int commentId, int userId);
        Task<bool> UserCanDeleteCommentAsync(int commentId, int userId);
        Task<int> GetTaskCommentCountAsync(int? projectTaskId = null, int? dailyTaskId = null);

        // Comment statistics
        Task<int> GetUserCommentCountAsync(int userId, DateTime? fromDate = null);
        Task<Dictionary<int, int>> GetTaskCommentCountsAsync(List<int> taskIds, bool isProjectTasks = true);
        Task<List<TaskCommentResponseDto>> GetMostRecentCommentsAsync(int count = 10);

        // Bulk operations
        Task<bool> BulkDeleteCommentsAsync(List<int> commentIds, string deletedByUser);
        Task<bool> DeleteAllTaskCommentsAsync(string deletedByUser, int? projectTaskId = null, int? dailyTaskId = null);
    }
}
