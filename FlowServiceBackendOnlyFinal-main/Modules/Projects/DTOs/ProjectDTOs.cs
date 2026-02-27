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
        public string Status { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Team members as user IDs
        public List<int> TeamMembers { get; set; } = new List<int>();

        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public List<ProjectColumnDto> Columns { get; set; } = new List<ProjectColumnDto>();
    }

    public class ProjectColumnDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string? Color { get; set; }
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

    public class ProjectStatisticsDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int OnHoldProjects { get; set; }
        public int HighPriorityCount { get; set; }
        public int MediumPriorityCount { get; set; }
        public int LowPriorityCount { get; set; }
    }

    // Request DTOs
    public class CreateProjectRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? ContactId { get; set; }

        // Team members as user IDs
        public List<int>? TeamMembers { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = "active";

        [StringLength(20)]
        public string? Priority { get; set; } = "medium";

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool CreateDefaultColumns { get; set; } = true;
    }

    public class UpdateProjectRequestDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public int? ContactId { get; set; }

        // Team members as user IDs
        public List<int>? TeamMembers { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ProjectSearchRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public int? ContactId { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedDate";
        public string? SortDirection { get; set; } = "desc";
    }
}
