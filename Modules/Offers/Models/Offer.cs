using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Offers.Models
{
    [Table("offers")]
    public class Offer
    {
        [Key]
        [Column("id")]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("title")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        // Contact Information
        [Required]
        [Column("contact_id")]
        public int ContactId { get; set; }

        // Financial Information
        [Required]
        [Column("amount", TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; } = 0;

        [Required]
        [Column("currency")]
        [MaxLength(3)]
        public string Currency { get; set; } = "TND";

        [Column("taxes", TypeName = "decimal(15,2)")]
        public decimal? Taxes { get; set; } = 0;

        [Column("discount", TypeName = "decimal(15,2)")]
        public decimal? Discount { get; set; } = 0;

        [Column("total_amount", TypeName = "decimal(15,2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalAmount { get; set; }

        // Status & Classification
        [Required]
        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "draft";

        [Column("category")]
        [MaxLength(50)]
        public string? Category { get; set; }

        [Column("source")]
        [MaxLength(50)]
        public string? Source { get; set; }

        // Billing Address
        [Column("billing_address")]
        public string? BillingAddress { get; set; }

        [Column("billing_postal_code")]
        [MaxLength(20)]
        public string? BillingPostalCode { get; set; }

        [Column("billing_country")]
        [MaxLength(100)]
        public string? BillingCountry { get; set; }

        // Delivery Address
        [Column("delivery_address")]
        public string? DeliveryAddress { get; set; }

        [Column("delivery_postal_code")]
        [MaxLength(20)]
        public string? DeliveryPostalCode { get; set; }

        [Column("delivery_country")]
        [MaxLength(100)]
        public string? DeliveryCountry { get; set; }

        // Validity
        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        // Assignment
        [Column("assigned_to")]
        [MaxLength(50)]
        public string? AssignedTo { get; set; }

        [Column("assigned_to_name")]
        [MaxLength(255)]
        public string? AssignedToName { get; set; }

        // Tags
        [Column("tags")]
        public string[]? Tags { get; set; }

        // Timestamps
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("created_by")]
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("last_activity")]
        public DateTime? LastActivity { get; set; }

        // Conversion Tracking
        [Column("converted_to_sale_id")]
        [MaxLength(50)]
        public string? ConvertedToSaleId { get; set; }

        [Column("converted_to_service_order_id")]
        [MaxLength(50)]
        public string? ConvertedToServiceOrderId { get; set; }

        [Column("converted_at")]
        public DateTime? ConvertedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<OfferItem>? Items { get; set; }
        public virtual ICollection<OfferActivity>? Activities { get; set; }
    }
}
