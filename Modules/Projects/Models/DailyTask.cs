using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Projects.Models
{
    /// <summary>
    /// DailyTask model matching database schema:
    /// Id, Title, Description, DueDate, IsCompleted, CompletedDate, AssignedUserId, Priority, Status, CreatedDate, CreatedBy
    /// </summary>
    public class DailyTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        public int? AssignedUserId { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        /// <summary>
        /// Task status: todo, in-progress, done
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "todo";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey("AssignedUserId")]
        public virtual MyApi.Modules.Users.Models.User? AssignedUser { get; set; }
    }
}
