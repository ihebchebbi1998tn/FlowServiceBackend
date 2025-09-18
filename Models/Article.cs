using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models
{
    public class Article
    {
        [Key]
        public Guid Id { get; set; }

        // Common fields
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty; // material | service

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        public string? Tags { get; set; } // JSON array
        public string? Notes { get; set; }

        // Material-specific (nullable for services)
        [StringLength(50)]
        public string? SKU { get; set; }
        public int? Stock { get; set; }
        public int? MinStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        [StringLength(200)]
        public string? Supplier { get; set; }
        [StringLength(200)]
        public string? Location { get; set; }
        [StringLength(200)]
        public string? SubLocation { get; set; }

        // Service-specific (nullable for materials)
        public decimal? BasePrice { get; set; }
        public int? Duration { get; set; }
        public string? SkillsRequired { get; set; } // JSON array
        public string? MaterialsNeeded { get; set; } // JSON array of material IDs
        public string? PreferredUsers { get; set; } // JSON array of user IDs
        public decimal? HourlyRate { get; set; }
        [StringLength(100)]
        public string? EstimatedDuration { get; set; }
        public bool? MaterialsIncluded { get; set; }
        [StringLength(200)]
        public string? WarrantyCoverage { get; set; }
        [StringLength(100)]
        public string? ServiceArea { get; set; }
        public string? Inclusions { get; set; } // JSON array
        public string? AddOnServices { get; set; } // JSON array

        // Usage tracking
        public DateTime? LastUsed { get; set; }
        [StringLength(100)]
        public string? LastUsedBy { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "system";
        [StringLength(100)]
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }
}