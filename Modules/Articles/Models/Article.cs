using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Articles.Models
{
    [Table("articles")]
    public class Article
    {
        [Key]
        [Column("id")]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("type")]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // 'material' or 'service'

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column("sku")]
        [MaxLength(100)]
        public string? Sku { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("category")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // available, low_stock, out_of_stock, discontinued, active, inactive

        // Material-specific fields
        [Column("stock")]
        public int? Stock { get; set; }

        [Column("min_stock")]
        public int? MinStock { get; set; }

        [Column("cost_price")]
        [Precision(10, 2)]
        public decimal? CostPrice { get; set; }

        [Column("sell_price")]
        [Precision(10, 2)]
        public decimal? SellPrice { get; set; }

        [Column("supplier")]
        [MaxLength(255)]
        public string? Supplier { get; set; }

        [Column("location")]
        [MaxLength(255)]
        public string? Location { get; set; }

        [Column("sub_location")]
        [MaxLength(255)]
        public string? SubLocation { get; set; }

        // Service-specific fields
        [Column("base_price")]
        [Precision(10, 2)]
        public decimal? BasePrice { get; set; }

        [Column("duration")]
        public int? Duration { get; set; } // in minutes

        [Column("skills_required")]
        public string? SkillsRequired { get; set; } // JSON array stored as string

        [Column("materials_needed")]
        public string? MaterialsNeeded { get; set; } // JSON array stored as string

        [Column("preferred_users")]
        public string? PreferredUsers { get; set; } // JSON array stored as string

        // Usage tracking
        [Column("last_used")]
        public DateTime? LastUsed { get; set; }

        [Column("last_used_by")]
        [MaxLength(50)]
        public string? LastUsedBy { get; set; }

        // Common fields
        [Column("tags")]
        public string? Tags { get; set; } // JSON array stored as string

        [Column("notes")]
        public string? Notes { get; set; }

        // Metadata
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Required]
        [Column("created_by")]
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        [Column("modified_by")]
        [MaxLength(50)]
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
