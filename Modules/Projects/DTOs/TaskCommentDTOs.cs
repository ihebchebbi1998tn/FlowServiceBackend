using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Projects.DTOs
{
    // Response DTOs
    public class TaskCommentResponseDto
    {
        public int Id { get; set; }
        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
    }

    public class TaskCommentListResponseDto
    {
        public List<TaskCommentResponseDto> Comments { get; set; } = new List<TaskCommentResponseDto>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // Request DTOs
    public class CreateTaskCommentRequestDto
    {
        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int AuthorId { get; set; }

        [Required]
        [StringLength(255)]
        public string AuthorName { get; set; } = string.Empty;
    }

    public class UpdateTaskCommentRequestDto
    {
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    public class TaskCommentSearchRequestDto
    {
        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }
        public int? AuthorId { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }
}
