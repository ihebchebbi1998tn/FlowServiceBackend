using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Projects.DTOs
{
    // Response DTOs
    public class ProjectTaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public int? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int ColumnId { get; set; }
        public string ColumnTitle { get; set; } = string.Empty;
        public string ColumnColor { get; set; } = string.Empty;
        public int Position { get; set; }
        public int? ParentTaskId { get; set; }
        public string? ParentTaskTitle { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Attachments { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public List<ProjectTaskResponseDto> SubTasks { get; set; } = new List<ProjectTaskResponseDto>();
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
    }

    public class DailyTaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int Position { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Attachments { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
    }

    public class TaskListResponseDto
    {
        public List<ProjectTaskResponseDto> ProjectTasks { get; set; } = new List<ProjectTaskResponseDto>();
        public List<DailyTaskResponseDto> DailyTasks { get; set; } = new List<DailyTaskResponseDto>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // Request DTOs
    public class CreateProjectTaskRequestDto
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public int? ContactId { get; set; }

        public int? AssigneeId { get; set; }

        [StringLength(255)]
        public string? AssigneeName { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "todo";

        [Required]
        [StringLength(10)]
        public string Priority { get; set; } = "medium";

        [Required]
        public int ColumnId { get; set; }

        public int? ParentTaskId { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }

        public decimal? EstimatedHours { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }

    public class CreateDailyTaskRequestDto
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "todo";

        [Required]
        [StringLength(10)]
        public string Priority { get; set; } = "medium";

        public DateTime? DueDate { get; set; }

        public decimal? EstimatedHours { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }

    public class UpdateProjectTaskRequestDto
    {
        [StringLength(255)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public int? ContactId { get; set; }

        public int? AssigneeId { get; set; }

        [StringLength(255)]
        public string? AssigneeName { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(10)]
        public string? Priority { get; set; }

        public int? ColumnId { get; set; }

        public int? Position { get; set; }

        public int? ParentTaskId { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }

        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }

        public List<string>? Tags { get; set; }

        public DateTime? CompletedAt { get; set; }
    }

    public class UpdateDailyTaskRequestDto
    {
        [StringLength(255)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(10)]
        public string? Priority { get; set; }

        public int? Position { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }

        public List<string>? Tags { get; set; }

        public DateTime? CompletedAt { get; set; }
    }

    public class MoveTaskRequestDto
    {
        [Required]
        public int ColumnId { get; set; }

        [Required]
        public int Position { get; set; }
    }

    public class BulkMoveTasksRequestDto
    {
        public List<TaskMoveDto> Tasks { get; set; } = new List<TaskMoveDto>();
    }

    public class TaskMoveDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ColumnId { get; set; }

        [Required]
        public int Position { get; set; }
    }

    public class TaskSearchRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public int? ProjectId { get; set; }
        public int? AssigneeId { get; set; }
        public int? ContactId { get; set; }
        public List<string>? Tags { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? HasParent { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }

    public class AssignTaskRequestDto
    {
        [Required]
        public int AssigneeId { get; set; }

        [Required]
        [StringLength(255)]
        public string AssigneeName { get; set; } = string.Empty;
    }

    public class BulkAssignTasksRequestDto
    {
        public List<int> TaskIds { get; set; } = new List<int>();

        [Required]
        public int AssigneeId { get; set; }

        [Required]
        [StringLength(255)]
        public string AssigneeName { get; set; } = string.Empty;
    }

    public class BulkUpdateTaskStatusDto
    {
        public List<int> TaskIds { get; set; } = new List<int>();

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;
    }
}
