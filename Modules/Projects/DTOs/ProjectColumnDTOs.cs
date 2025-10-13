using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Projects.DTOs
{
    // Response DTOs
    public class ProjectColumnResponseDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Position { get; set; }
        public bool IsDefault { get; set; }
        public int? TaskLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TaskCount { get; set; }
    }

    public class ProjectColumnListResponseDto
    {
        public List<ProjectColumnResponseDto> Columns { get; set; } = new List<ProjectColumnResponseDto>();
        public int TotalCount { get; set; }
    }

    // Request DTOs
    public class CreateProjectColumnRequestDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(7)]
        [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex color code")]
        public string Color { get; set; } = "#3b82f6";

        [Required]
        public int Position { get; set; }

        public bool IsDefault { get; set; } = false;

        public int? TaskLimit { get; set; }
    }

    public class UpdateProjectColumnRequestDto
    {
        [StringLength(255)]
        public string? Title { get; set; }

        [StringLength(7)]
        [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex color code")]
        public string? Color { get; set; }

        public int? Position { get; set; }

        public bool? IsDefault { get; set; }

        public int? TaskLimit { get; set; }
    }

    public class ReorderProjectColumnsRequestDto
    {
        public List<ProjectColumnPositionDto> Columns { get; set; } = new List<ProjectColumnPositionDto>();
    }

    public class ProjectColumnPositionDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int Position { get; set; }
    }

    public class BulkDeleteProjectColumnsDto
    {
        public List<int> ColumnIds { get; set; } = new List<int>();
        public int? MoveTasksToColumnId { get; set; }
    }
}
