using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Projects.Models
{
    public class TaskComment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int AuthorId { get; set; }

        [Required]
        [StringLength(255)]
        public string AuthorName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("ProjectTaskId")]
        public virtual ProjectTask? ProjectTask { get; set; }

        [ForeignKey("DailyTaskId")]
        public virtual DailyTask? DailyTask { get; set; }
    }
}
