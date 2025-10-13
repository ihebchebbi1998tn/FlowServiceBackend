using MyApi.Modules.Projects.DTOs;

namespace MyApi.Modules.Projects.Services
{
    public interface ITaskAttachmentService
    {
        // Attachment CRUD operations
        Task<TaskAttachmentListResponseDto> GetTaskAttachmentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20);
        Task<TaskAttachmentResponseDto?> GetAttachmentByIdAsync(int id);
        Task<TaskAttachmentResponseDto> CreateAttachmentAsync(CreateTaskAttachmentRequestDto createDto, string createdByUser);
        Task<TaskAttachmentResponseDto?> UpdateAttachmentAsync(int id, UpdateTaskAttachmentRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteAttachmentAsync(int id, string deletedByUser);

        // Attachment search and filtering
        Task<TaskAttachmentListResponseDto> SearchAttachmentsAsync(TaskAttachmentSearchRequestDto searchRequest);
        Task<TaskAttachmentListResponseDto> GetAttachmentsByUploaderAsync(int uploaderId, int pageNumber = 1, int pageSize = 20);
        Task<TaskAttachmentListResponseDto> GetAttachmentsByTypeAsync(string mimeType, int pageNumber = 1, int pageSize = 20);
        Task<TaskAttachmentListResponseDto> GetImageAttachmentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20);
        Task<TaskAttachmentListResponseDto> GetDocumentAttachmentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20);

        // Attachment validation and utilities
        Task<bool> AttachmentExistsAsync(int id);
        Task<bool> UserCanAccessAttachmentAsync(int attachmentId, int userId);
        Task<bool> UserCanEditAttachmentAsync(int attachmentId, int userId);
        Task<bool> UserCanDeleteAttachmentAsync(int attachmentId, int userId);
        Task<int> GetTaskAttachmentCountAsync(int? projectTaskId = null, int? dailyTaskId = null);
        Task<long> GetTaskAttachmentsTotalSizeAsync(int? projectTaskId = null, int? dailyTaskId = null);

        // File management utilities
        Task<bool> IsValidFileTypeAsync(string mimeType);
        Task<bool> IsFileSizeValidAsync(long fileSize);
        Task<string> GetFileTypeIconAsync(string mimeType);
        Task<string> FormatFileSizeAsync(long bytes);
        Task<bool> IsImageFileAsync(string mimeType);
        Task<bool> IsDocumentFileAsync(string mimeType);

        // Attachment statistics
        Task<int> GetUserUploadCountAsync(int userId, DateTime? fromDate = null);
        Task<long> GetUserUploadSizeAsync(int userId, DateTime? fromDate = null);
        Task<Dictionary<int, int>> GetTaskAttachmentCountsAsync(List<int> taskIds, bool isProjectTasks = true);
        Task<List<TaskAttachmentResponseDto>> GetMostRecentAttachmentsAsync(int count = 10);

        // Bulk operations
        Task<bool> BulkDeleteAttachmentsAsync(BulkDeleteTaskAttachmentsDto bulkDeleteDto, string deletedByUser);
        Task<bool> DeleteAllTaskAttachmentsAsync(string deletedByUser, int? projectTaskId = null, int? dailyTaskId = null);
        Task<long> GetTotalAttachmentsSizeAsync();
        Task<bool> CleanupOrphanedAttachmentsAsync(string deletedByUser);
    }
}
