using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Projects.Models
{
    public class ProjectColumn
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(7)]
        public string Color { get; set; } = "#3b82f6"; // Hex color code

        [Required]
        public int Position { get; set; }

        public bool IsDefault { get; set; } = false;

        public int? TaskLimit { get; set; } // WIP limit

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}
