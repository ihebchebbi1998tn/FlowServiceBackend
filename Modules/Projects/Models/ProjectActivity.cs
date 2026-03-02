using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Projects.Models
{
    /// <summary>
    /// ProjectActivity model for tracking project changes and events
    /// </summary>
    public class ProjectActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty; // created, updated, task_added, task_completed, member_added, member_removed, etc.

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Details { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(255)]
        public string CreatedBy { get; set; } = string.Empty;

        // Optional reference to related entity (TaskId, NoteId, etc.)
        public int? RelatedEntityId { get; set; }

        [StringLength(100)]
        public string? RelatedEntityType { get; set; }

        // Navigation property
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }
    }
}
