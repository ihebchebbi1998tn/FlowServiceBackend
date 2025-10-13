using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Modules.Contacts.Models;

namespace MyApi.Modules.Projects.Models
{
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? ContactId { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        [StringLength(255)]
        public string OwnerName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? TeamMembers { get; set; } // JSON array of user IDs

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        [StringLength(3)]
        public string? Currency { get; set; } // Currency code (USD, EUR, etc.)

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "active"; // active, completed, on-hold, cancelled

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "service"; // service, sales, internal, custom

        [Required]
        [StringLength(10)]
        public string Priority { get; set; } = "medium"; // low, medium, high, urgent

        public int Progress { get; set; } = 0; // 0-100

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        [StringLength(1000)]
        public string? Tags { get; set; } // JSON array of tags

        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual ICollection<ProjectColumn> Columns { get; set; } = new List<ProjectColumn>();
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public virtual Contact? Contact { get; set; }
    }
}
