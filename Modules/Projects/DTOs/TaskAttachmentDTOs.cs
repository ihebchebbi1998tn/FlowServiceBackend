using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Projects.DTOs
{
    // Response DTOs
    public class TaskAttachmentResponseDto
    {
        public int Id { get; set; }
        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public int UploadedBy { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? Caption { get; set; }
        public bool IsImage { get; set; }
        public bool IsDocument { get; set; }
    }

    public class TaskAttachmentListResponseDto
    {
        public List<TaskAttachmentResponseDto> Attachments { get; set; } = new List<TaskAttachmentResponseDto>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public long TotalSize { get; set; }
        public string TotalSizeFormatted { get; set; } = string.Empty;
    }

    // Request DTOs
    public class CreateTaskAttachmentRequestDto
    {
        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MimeType { get; set; }

        public long FileSize { get; set; }

        [Required]
        public int UploadedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string UploadedByName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Caption { get; set; }
    }

    public class UpdateTaskAttachmentRequestDto
    {
        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        [StringLength(500)]
        public string? Caption { get; set; }
    }

    public class TaskAttachmentSearchRequestDto
    {
        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }
        public int? UploadedBy { get; set; }
        public string? SearchTerm { get; set; }
        public string? MimeType { get; set; }
        public bool? IsImage { get; set; }
        public bool? IsDocument { get; set; }
        public DateTime? UploadedFrom { get; set; }
        public DateTime? UploadedTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "UploadedAt";
        public string? SortDirection { get; set; } = "desc";
    }

    public class BulkDeleteTaskAttachmentsDto
    {
        public List<int> AttachmentIds { get; set; } = new List<int>();
    }
}
