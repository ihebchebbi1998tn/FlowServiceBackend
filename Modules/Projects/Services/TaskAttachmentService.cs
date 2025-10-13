using MyApi.Data;
using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Projects.Services
{
    public class TaskAttachmentService : ITaskAttachmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskAttachmentService> _logger;

        // Valid file types for attachments
        private readonly HashSet<string> _validImageTypes = new()
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/svg+xml"
        };

        private readonly HashSet<string> _validDocumentTypes = new()
        {
            "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "text/plain", "text/csv", "application/json", "application/xml"
        };

        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public TaskAttachmentService(ApplicationDbContext context, ILogger<TaskAttachmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskAttachmentListResponseDto> GetTaskAttachmentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskAttachments.Where(a => !a.IsDeleted);

                if (projectTaskId.HasValue)
                    query = query.Where(a => a.ProjectTaskId == projectTaskId.Value);

                if (dailyTaskId.HasValue)
                    query = query.Where(a => a.DailyTaskId == dailyTaskId.Value);

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var attachments = await query
                    .OrderByDescending(a => a.UploadedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var attachmentDtos = attachments.Select(MapToAttachmentDto).ToList();
                var totalSize = await query.SumAsync(a => a.FileSize);

                return new TaskAttachmentListResponseDto
                {
                    Attachments = attachmentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1,
                    TotalSize = totalSize,
                    TotalSizeFormatted = await FormatFileSizeAsync(totalSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task attachments");
                throw;
            }
        }

        public async Task<TaskAttachmentResponseDto?> GetAttachmentByIdAsync(int id)
        {
            try
            {
                var attachment = await _context.TaskAttachments
                    .Include(a => a.ProjectTask)
                    .Include(a => a.DailyTask)
                    .Where(a => a.Id == id && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                return attachment != null ? MapToAttachmentDto(attachment) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachment by id {AttachmentId}", id);
                throw;
            }
        }

        public async Task<TaskAttachmentResponseDto> CreateAttachmentAsync(CreateTaskAttachmentRequestDto createDto, string createdByUser)
        {
            try
            {
                // Validate file type and size
                if (!await IsValidFileTypeAsync(createDto.MimeType ?? string.Empty))
                    throw new InvalidOperationException("Invalid file type");

                if (!await IsFileSizeValidAsync(createDto.FileSize))
                    throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB");

                // Validate that either projectTaskId or dailyTaskId is provided
                if (!createDto.ProjectTaskId.HasValue && !createDto.DailyTaskId.HasValue)
                    throw new InvalidOperationException("Either ProjectTaskId or DailyTaskId must be provided");

                if (createDto.ProjectTaskId.HasValue && createDto.DailyTaskId.HasValue)
                    throw new InvalidOperationException("Cannot specify both ProjectTaskId and DailyTaskId");

                // Validate task exists
                if (createDto.ProjectTaskId.HasValue)
                {
                    var projectTaskExists = await _context.ProjectTasks.AnyAsync(t => t.Id == createDto.ProjectTaskId.Value && !t.IsDeleted);
                    if (!projectTaskExists)
                        throw new InvalidOperationException("Project task not found");
                }

                if (createDto.DailyTaskId.HasValue)
                {
                    var dailyTaskExists = await _context.DailyTasks.AnyAsync(t => t.Id == createDto.DailyTaskId.Value && !t.IsDeleted);
                    if (!dailyTaskExists)
                        throw new InvalidOperationException("Daily task not found");
                }

                var attachment = new TaskAttachment
                {
                    ProjectTaskId = createDto.ProjectTaskId,
                    DailyTaskId = createDto.DailyTaskId,
                    FileName = createDto.FileName,
                    OriginalFileName = createDto.OriginalFileName,
                    FileUrl = createDto.FileUrl,
                    MimeType = createDto.MimeType,
                    FileSize = createDto.FileSize,
                    UploadedBy = createDto.UploadedBy,
                    UploadedByName = createDto.UploadedByName,
                    Caption = createDto.Caption,
                    UploadedAt = DateTime.UtcNow
                };

                _context.TaskAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Attachment created successfully with ID {AttachmentId}", attachment.Id);
                return MapToAttachmentDto(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attachment");
                throw;
            }
        }

        public async Task<TaskAttachmentResponseDto?> UpdateAttachmentAsync(int id, UpdateTaskAttachmentRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var attachment = await _context.TaskAttachments
                    .Where(a => a.Id == id && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                if (attachment == null)
                    return null;

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.OriginalFileName))
                    attachment.OriginalFileName = updateDto.OriginalFileName;

                if (updateDto.Caption != null)
                    attachment.Caption = updateDto.Caption;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Attachment updated successfully with ID {AttachmentId}", id);
                return MapToAttachmentDto(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attachment with ID {AttachmentId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAttachmentAsync(int id, string deletedByUser)
        {
            try
            {
                var attachment = await _context.TaskAttachments
                    .Where(a => a.Id == id && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                if (attachment == null)
                    return false;

                attachment.IsDeleted = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Attachment deleted successfully with ID {AttachmentId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment with ID {AttachmentId}", id);
                throw;
            }
        }

        public async Task<TaskAttachmentListResponseDto> SearchAttachmentsAsync(TaskAttachmentSearchRequestDto searchRequest)
        {
            return await GetTaskAttachmentsAsync(searchRequest.ProjectTaskId, searchRequest.DailyTaskId, searchRequest.PageNumber, searchRequest.PageSize);
        }

        public async Task<TaskAttachmentListResponseDto> GetAttachmentsByUploaderAsync(int uploaderId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskAttachments
                    .Where(a => a.UploadedBy == uploaderId && !a.IsDeleted);

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var attachments = await query
                    .OrderByDescending(a => a.UploadedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var attachmentDtos = attachments.Select(MapToAttachmentDto).ToList();
                var totalSize = await query.SumAsync(a => a.FileSize);

                return new TaskAttachmentListResponseDto
                {
                    Attachments = attachmentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1,
                    TotalSize = totalSize,
                    TotalSizeFormatted = await FormatFileSizeAsync(totalSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachments by uploader {UploaderId}", uploaderId);
                throw;
            }
        }

        public async Task<TaskAttachmentListResponseDto> GetAttachmentsByTypeAsync(string mimeType, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskAttachments
                    .Where(a => a.MimeType == mimeType && !a.IsDeleted);

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var attachments = await query
                    .OrderByDescending(a => a.UploadedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var attachmentDtos = attachments.Select(MapToAttachmentDto).ToList();
                var totalSize = await query.SumAsync(a => a.FileSize);

                return new TaskAttachmentListResponseDto
                {
                    Attachments = attachmentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1,
                    TotalSize = totalSize,
                    TotalSizeFormatted = await FormatFileSizeAsync(totalSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachments by type {MimeType}", mimeType);
                throw;
            }
        }

        public async Task<TaskAttachmentListResponseDto> GetImageAttachmentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskAttachments.Where(a => !a.IsDeleted);

                if (projectTaskId.HasValue)
                    query = query.Where(a => a.ProjectTaskId == projectTaskId.Value);

                if (dailyTaskId.HasValue)
                    query = query.Where(a => a.DailyTaskId == dailyTaskId.Value);

                query = query.Where(a => a.MimeType != null && _validImageTypes.Contains(a.MimeType));

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var attachments = await query
                    .OrderByDescending(a => a.UploadedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var attachmentDtos = attachments.Select(MapToAttachmentDto).ToList();
                var totalSize = await query.SumAsync(a => a.FileSize);

                return new TaskAttachmentListResponseDto
                {
                    Attachments = attachmentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1,
                    TotalSize = totalSize,
                    TotalSizeFormatted = await FormatFileSizeAsync(totalSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image attachments");
                throw;
            }
        }

        public async Task<TaskAttachmentListResponseDto> GetDocumentAttachmentsAsync(int? projectTaskId = null, int? dailyTaskId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.TaskAttachments.Where(a => !a.IsDeleted);

                if (projectTaskId.HasValue)
                    query = query.Where(a => a.ProjectTaskId == projectTaskId.Value);

                if (dailyTaskId.HasValue)
                    query = query.Where(a => a.DailyTaskId == dailyTaskId.Value);

                query = query.Where(a => a.MimeType != null && _validDocumentTypes.Contains(a.MimeType));

                var totalCount = await query.CountAsync();
                var skip = (pageNumber - 1) * pageSize;

                var attachments = await query
                    .OrderByDescending(a => a.UploadedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var attachmentDtos = attachments.Select(MapToAttachmentDto).ToList();
                var totalSize = await query.SumAsync(a => a.FileSize);

                return new TaskAttachmentListResponseDto
                {
                    Attachments = attachmentDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1,
                    TotalSize = totalSize,
                    TotalSizeFormatted = await FormatFileSizeAsync(totalSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document attachments");
                throw;
            }
        }

        public async Task<bool> AttachmentExistsAsync(int id)
        {
            return await _context.TaskAttachments.AnyAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<bool> UserCanAccessAttachmentAsync(int attachmentId, int userId)
        {
            // Basic implementation - can be enhanced with proper authorization
            return true;
        }

        public async Task<bool> UserCanEditAttachmentAsync(int attachmentId, int userId)
        {
            var attachment = await _context.TaskAttachments
                .Where(a => a.Id == attachmentId && !a.IsDeleted)
                .FirstOrDefaultAsync();

            return attachment?.UploadedBy == userId;
        }

        public async Task<bool> UserCanDeleteAttachmentAsync(int attachmentId, int userId)
        {
            return await UserCanEditAttachmentAsync(attachmentId, userId);
        }

        public async Task<int> GetTaskAttachmentCountAsync(int? projectTaskId = null, int? dailyTaskId = null)
        {
            var query = _context.TaskAttachments.Where(a => !a.IsDeleted);

            if (projectTaskId.HasValue)
                query = query.Where(a => a.ProjectTaskId == projectTaskId.Value);

            if (dailyTaskId.HasValue)
                query = query.Where(a => a.DailyTaskId == dailyTaskId.Value);

            return await query.CountAsync();
        }

        public async Task<long> GetTaskAttachmentsTotalSizeAsync(int? projectTaskId = null, int? dailyTaskId = null)
        {
            var query = _context.TaskAttachments.Where(a => !a.IsDeleted);

            if (projectTaskId.HasValue)
                query = query.Where(a => a.ProjectTaskId == projectTaskId.Value);

            if (dailyTaskId.HasValue)
                query = query.Where(a => a.DailyTaskId == dailyTaskId.Value);

            return await query.SumAsync(a => a.FileSize);
        }

        public async Task<bool> IsValidFileTypeAsync(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return false;

            return await Task.FromResult(_validImageTypes.Contains(mimeType) || _validDocumentTypes.Contains(mimeType));
        }

        public async Task<bool> IsFileSizeValidAsync(long fileSize)
        {
            return await Task.FromResult(fileSize > 0 && fileSize <= MaxFileSize);
        }

        public async Task<string> GetFileTypeIconAsync(string mimeType)
        {
            return await Task.FromResult(mimeType switch
            {
                var type when type.StartsWith("image/") => "🖼️",
                "application/pdf" => "📄",
                var type when type.Contains("word") => "📝",
                var type when type.Contains("excel") || type.Contains("spreadsheet") => "📊",
                var type when type.Contains("powerpoint") || type.Contains("presentation") => "📊",
                "text/plain" => "📄",
                "application/json" => "🗂️",
                _ => "📎"
            });
        }

        public async Task<string> FormatFileSizeAsync(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return await Task.FromResult($"{number:n1} {suffixes[counter]}");
        }

        public async Task<bool> IsImageFileAsync(string mimeType)
        {
            return await Task.FromResult(!string.IsNullOrEmpty(mimeType) && _validImageTypes.Contains(mimeType));
        }

        public async Task<bool> IsDocumentFileAsync(string mimeType)
        {
            return await Task.FromResult(!string.IsNullOrEmpty(mimeType) && _validDocumentTypes.Contains(mimeType));
        }

        public async Task<int> GetUserUploadCountAsync(int userId, DateTime? fromDate = null)
        {
            var query = _context.TaskAttachments
                .Where(a => a.UploadedBy == userId && !a.IsDeleted);

            if (fromDate.HasValue)
                query = query.Where(a => a.UploadedAt >= fromDate.Value);

            return await query.CountAsync();
        }

        public async Task<long> GetUserUploadSizeAsync(int userId, DateTime? fromDate = null)
        {
            var query = _context.TaskAttachments
                .Where(a => a.UploadedBy == userId && !a.IsDeleted);

            if (fromDate.HasValue)
                query = query.Where(a => a.UploadedAt >= fromDate.Value);

            return await query.SumAsync(a => a.FileSize);
        }

        public async Task<Dictionary<int, int>> GetTaskAttachmentCountsAsync(List<int> taskIds, bool isProjectTasks = true)
        {
            var query = _context.TaskAttachments.Where(a => !a.IsDeleted);

            if (isProjectTasks)
                query = query.Where(a => a.ProjectTaskId.HasValue && taskIds.Contains(a.ProjectTaskId.Value));
            else
                query = query.Where(a => a.DailyTaskId.HasValue && taskIds.Contains(a.DailyTaskId.Value));

            return await query
                .GroupBy(a => isProjectTasks ? a.ProjectTaskId!.Value : a.DailyTaskId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<List<TaskAttachmentResponseDto>> GetMostRecentAttachmentsAsync(int count = 10)
        {
            var attachments = await _context.TaskAttachments
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.UploadedAt)
                .Take(count)
                .ToListAsync();

            return attachments.Select(MapToAttachmentDto).ToList();
        }

        public async Task<bool> BulkDeleteAttachmentsAsync(BulkDeleteTaskAttachmentsDto bulkDeleteDto, string deletedByUser)
        {
            try
            {
                var attachments = await _context.TaskAttachments
                    .Where(a => bulkDeleteDto.AttachmentIds.Contains(a.Id) && !a.IsDeleted)
                    .ToListAsync();

                foreach (var attachment in attachments)
                {
                    attachment.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk deleted {Count} attachments", attachments.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk attachment deletion");
                throw;
            }
        }

        public async Task<bool> DeleteAllTaskAttachmentsAsync(string deletedByUser, int? projectTaskId = null, int? dailyTaskId = null)
        {
            try
            {
                var query = _context.TaskAttachments.Where(a => !a.IsDeleted);

                if (projectTaskId.HasValue)
                    query = query.Where(a => a.ProjectTaskId == projectTaskId.Value);

                if (dailyTaskId.HasValue)
                    query = query.Where(a => a.DailyTaskId == dailyTaskId.Value);

                var attachments = await query.ToListAsync();

                foreach (var attachment in attachments)
                {
                    attachment.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted all attachments for task");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all task attachments");
                throw;
            }
        }

        public async Task<long> GetTotalAttachmentsSizeAsync()
        {
            return await _context.TaskAttachments
                .Where(a => !a.IsDeleted)
                .SumAsync(a => a.FileSize);
        }

        public async Task<bool> CleanupOrphanedAttachmentsAsync(string deletedByUser)
        {
            try
            {
                // Find attachments with no associated tasks
                var orphanedAttachments = await _context.TaskAttachments
                    .Where(a => !a.IsDeleted &&
                               (a.ProjectTaskId.HasValue && !_context.ProjectTasks.Any(t => t.Id == a.ProjectTaskId.Value && !t.IsDeleted)) ||
                               (a.DailyTaskId.HasValue && !_context.DailyTasks.Any(t => t.Id == a.DailyTaskId.Value && !t.IsDeleted)))
                    .ToListAsync();

                foreach (var attachment in orphanedAttachments)
                {
                    attachment.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} orphaned attachments", orphanedAttachments.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned attachments cleanup");
                throw;
            }
        }

        private TaskAttachmentResponseDto MapToAttachmentDto(TaskAttachment attachment)
        {
            return new TaskAttachmentResponseDto
            {
                Id = attachment.Id,
                ProjectTaskId = attachment.ProjectTaskId,
                DailyTaskId = attachment.DailyTaskId,
                TaskTitle = attachment.ProjectTask?.Title ?? attachment.DailyTask?.Title ?? string.Empty,
                FileName = attachment.FileName,
                OriginalFileName = attachment.OriginalFileName,
                FileUrl = attachment.FileUrl,
                MimeType = attachment.MimeType,
                FileSize = attachment.FileSize,
                FileSizeFormatted = FormatFileSizeAsync(attachment.FileSize).Result,
                UploadedBy = attachment.UploadedBy,
                UploadedByName = attachment.UploadedByName,
                UploadedAt = attachment.UploadedAt,
                Caption = attachment.Caption,
                IsImage = IsImageFileAsync(attachment.MimeType ?? string.Empty).Result,
                IsDocument = IsDocumentFileAsync(attachment.MimeType ?? string.Empty).Result
            };
        }
    }
}
