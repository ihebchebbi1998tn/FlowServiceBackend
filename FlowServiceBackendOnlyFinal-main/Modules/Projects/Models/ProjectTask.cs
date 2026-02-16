using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Projects.Models
{
    /// <summary>
    /// ProjectTask model matching database schema:
    /// Id, ProjectId, ColumnId, Title, Description, Priority, DueDate, AssignedUserId, DisplayOrder, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
    /// </summary>
    public class ProjectTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int ColumnId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? AssignedUserId { get; set; }

        [Required]
        public int DisplayOrder { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("ColumnId")]
        public virtual ProjectColumn Column { get; set; } = null!;

        [ForeignKey("AssignedUserId")]
        public virtual MyApi.Modules.Users.Models.User? AssignedUser { get; set; }

        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public virtual ICollection<TaskAttachment> TaskAttachments { get; set; } = new List<TaskAttachment>();
    }
}
