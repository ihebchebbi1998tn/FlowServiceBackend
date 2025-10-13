using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Projects.DTOs
{
    // Response DTOs
    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public List<int> TeamMembers { get; set; } = new List<int>();
        public List<string> TeamMemberNames { get; set; } = new List<string>();
        public decimal? Budget { get; set; }
        public string? Currency { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int Progress { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public List<ProjectColumnResponseDto> Columns { get; set; } = new List<ProjectColumnResponseDto>();
        public ProjectStatsDto Stats { get; set; } = new ProjectStatsDto();
    }

    public class ProjectListResponseDto
    {
        public List<ProjectResponseDto> Projects { get; set; } = new List<ProjectResponseDto>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class ProjectStatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int ActiveMembers { get; set; }
        public decimal CompletionPercentage { get; set; }
    }

    // Request DTOs
    public class CreateProjectRequestDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? ContactId { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        [StringLength(255)]
        public string OwnerName { get; set; } = string.Empty;

        public List<int> TeamMembers { get; set; } = new List<int>();

        public decimal? Budget { get; set; }

        [StringLength(3)]
        public string? Currency { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "active";

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "service";

        [Required]
        [StringLength(10)]
        public string Priority { get; set; } = "medium";

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }

    public class UpdateProjectRequestDto
    {
        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? ContactId { get; set; }

        public int? OwnerId { get; set; }

        [StringLength(255)]
        public string? OwnerName { get; set; }

        public List<int>? TeamMembers { get; set; }

        public decimal? Budget { get; set; }

        [StringLength(3)]
        public string? Currency { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(10)]
        public string? Priority { get; set; }

        public int? Progress { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        public List<string>? Tags { get; set; }

        public bool? IsArchived { get; set; }
    }

    public class ProjectSearchRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public int? OwnerId { get; set; }
        public int? ContactId { get; set; }
        public List<int>? TeamMemberIds { get; set; }
        public List<string>? Tags { get; set; }
        public bool? IsArchived { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }

    public class BulkUpdateProjectStatusDto
    {
        public List<int> ProjectIds { get; set; } = new List<int>();
        public string Status { get; set; } = string.Empty;
    }

    public class AssignTeamMemberDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string UserName { get; set; } = string.Empty;
    }

    public class RemoveTeamMemberDto
    {
        [Required]
        public int UserId { get; set; }
    }
}
