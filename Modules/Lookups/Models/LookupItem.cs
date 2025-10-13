using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Lookups.Models
{
    [Table("LookupItems")]
    public class LookupItem
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        [Required]
        [MaxLength(50)]
        public string LookupType { get; set; } = string.Empty; // article-category, task-status, etc.

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        [Required]
        [MaxLength(100)]
        public string CreatedUser { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ModifyUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Type-specific properties (nullable for types that don't use them)
        public int? Level { get; set; } // for priorities
        public bool? IsCompleted { get; set; } // for statuses
        public int? DefaultDuration { get; set; } // for event types (in minutes)
        public bool? IsAvailable { get; set; } // for technician statuses
        public bool? IsPaid { get; set; } // for leave types
        
        [MaxLength(100)]
        public string? Category { get; set; } // for skills
    }
}
