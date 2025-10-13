using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Modules.Contacts.Models;

namespace MyApi.Modules.Projects.Models
{
    public class ProjectTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

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
        public string Status { get; set; } = "todo"; // References task statuses lookup

        [Required]
        [StringLength(10)]
        public string Priority { get; set; } = "medium"; // low, medium, high, urgent

        [Required]
        public int ColumnId { get; set; }

        [Required]
        public int Position { get; set; }

        public int? ParentTaskId { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ActualHours { get; set; }

        [StringLength(1000)]
        public string? Tags { get; set; } // JSON array of tags

        [StringLength(2000)]
        public string? Attachments { get; set; } // JSON array of attachment URLs

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("ColumnId")]
        public virtual ProjectColumn Column { get; set; } = null!;

        [ForeignKey("ContactId")]
        public virtual Contact? Contact { get; set; }

        [ForeignKey("ParentTaskId")]
        public virtual ProjectTask? ParentTask { get; set; }

        public virtual ICollection<ProjectTask> SubTasks { get; set; } = new List<ProjectTask>();
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public virtual ICollection<TaskAttachment> TaskAttachments { get; set; } = new List<TaskAttachment>();
    }
}
