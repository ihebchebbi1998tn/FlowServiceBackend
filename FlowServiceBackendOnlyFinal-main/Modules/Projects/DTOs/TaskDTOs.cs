using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Projects.DTOs
{
    // Response DTOs - matching actual database schema
    public class ProjectTaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int ColumnId { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string? ColumnColor { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
    }

    public class DailyTaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public string? Priority { get; set; }
        /// <summary>
        /// Task status: todo, in-progress, done
        /// </summary>
        public string Status { get; set; } = "todo";
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
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
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int ColumnId { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? AssignedUserId { get; set; }

        public int? DisplayOrder { get; set; }
    }

    public class CreateDailyTaskRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public int? AssignedUserId { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        /// <summary>
        /// Task status: todo, in-progress, done. Defaults to "todo"
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }
    }

    public class UpdateProjectTaskRequestDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public int? ColumnId { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? AssignedUserId { get; set; }

        public int? DisplayOrder { get; set; }
    }

    public class UpdateDailyTaskRequestDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public bool? IsCompleted { get; set; }

        public DateTime? CompletedDate { get; set; }

        public int? AssignedUserId { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        /// <summary>
        /// Task status: todo, in-progress, done
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }
    }

    public class MoveTaskRequestDto
    {
        [Required]
        public int ColumnId { get; set; }

        [Required]
        public int DisplayOrder { get; set; }
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
        public int DisplayOrder { get; set; }
    }

    public class TaskSearchRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? Priority { get; set; }
        public int? ProjectId { get; set; }
        public int? ColumnId { get; set; }
        public int? AssignedUserId { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedDate";
        public string? SortDirection { get; set; } = "desc";
    }

    public class AssignTaskRequestDto
    {
        public int? AssignedUserId { get; set; }
    }

    public class BulkAssignTasksRequestDto
    {
        public List<int> TaskIds { get; set; } = new List<int>();
        public int? AssignedUserId { get; set; }
    }

    public class BulkUpdateTaskStatusDto
    {
        public List<int> TaskIds { get; set; } = new List<int>();
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;
    }

    public class TaskStatisticsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TasksDueToday { get; set; }
        public int TasksDueThisWeek { get; set; }
        public Dictionary<string, int> TasksByPriority { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TasksByColumn { get; set; } = new Dictionary<string, int>();
    }
}
