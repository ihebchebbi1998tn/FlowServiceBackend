using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Offers.Models
{
    [Table("offer_items")]
    public class OfferItem
    {
        [Key]
        [Column("id")]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("offer_id")]
        [MaxLength(50)]
        public string OfferId { get; set; } = string.Empty;

        // Item Information
        [Required]
        [Column("type")]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Column("article_id")]
        [MaxLength(50)]
        public string ArticleId { get; set; } = string.Empty;

        [Required]
        [Column("item_name")]
        [MaxLength(255)]
        public string ItemName { get; set; } = string.Empty;

        [Column("item_code")]
        [MaxLength(100)]
        public string? ItemCode { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        // Quantity & Pricing
        [Required]
        [Column("quantity", TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; } = 1;

        [Required]
        [Column("unit_price", TypeName = "decimal(15,2)")]
        public decimal UnitPrice { get; set; } = 0;

        // Discount
        [Column("discount", TypeName = "decimal(15,2)")]
        public decimal Discount { get; set; } = 0;

        [Column("discount_type")]
        [MaxLength(20)]
        public string DiscountType { get; set; } = "percentage";

        // Calculated total price
        [Column("total_price", TypeName = "decimal(15,2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalPrice { get; set; }

        // Installation reference
        [Column("installation_id")]
        [MaxLength(50)]
        public string? InstallationId { get; set; }

        [Column("installation_name")]
        [MaxLength(255)]
        public string? InstallationName { get; set; }

        // Navigation Property
        [ForeignKey("OfferId")]
        public virtual Offer? Offer { get; set; }
    }
}
