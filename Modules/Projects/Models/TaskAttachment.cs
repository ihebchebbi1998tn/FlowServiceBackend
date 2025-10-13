using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Projects.Models
{
    public class TaskAttachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ProjectTaskId { get; set; }
        public int? DailyTaskId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MimeType { get; set; }

        public long FileSize { get; set; }

        [Required]
        public int UploadedBy { get; set; }

        [Required]
        [StringLength(255)]
        public string UploadedByName { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Caption { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("ProjectTaskId")]
        public virtual ProjectTask? ProjectTask { get; set; }

        [ForeignKey("DailyTaskId")]
        public virtual DailyTask? DailyTask { get; set; }
    }
}
