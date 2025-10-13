using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Articles.Models
{
    [Table("inventory_transactions")]
    public class InventoryTransaction
    {
        [Key]
        [Column("id")]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("article_id")]
        [MaxLength(50)]
        public string ArticleId { get; set; } = string.Empty;

        [Required]
        [Column("type")]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // 'in', 'out', 'transfer', 'adjustment'

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("from_location")]
        [MaxLength(255)]
        public string? FromLocation { get; set; }

        [Column("to_location")]
        [MaxLength(255)]
        public string? ToLocation { get; set; }

        [Required]
        [Column("reason")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Column("reference")]
        [MaxLength(100)]
        public string? Reference { get; set; } // Service order ID, purchase order, etc.

        [Required]
        [Column("performed_by")]
        [MaxLength(50)]
        public string PerformedBy { get; set; } = string.Empty;

        [Column("notes")]
        public string? Notes { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
