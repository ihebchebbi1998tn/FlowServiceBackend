using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Articles.Models
{
    [Table("Articles")]
    public class Article
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ArticleNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = "piece";

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalesPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinStockLevel { get; set; }

        public int? LocationId { get; set; }

        [MaxLength(200)]
        public string? Supplier { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "material";

        public int? Duration { get; set; }  // Duration in minutes for service-type articles

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [MaxLength(100)]
        public string? ModifiedBy { get; set; }
    }
}
