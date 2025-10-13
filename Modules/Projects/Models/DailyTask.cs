using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Projects.Models
{
    public class DailyTask
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
        public int UserId { get; set; }

        [StringLength(255)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "todo"; // todo, in-progress, completed

        [Required]
        [StringLength(10)]
        public string Priority { get; set; } = "medium"; // low, medium, high, urgent

        [Required]
        public int Position { get; set; }

        public DateTime? DueDate { get; set; }

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
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public virtual ICollection<TaskAttachment> TaskAttachments { get; set; } = new List<TaskAttachment>();
    }
}
