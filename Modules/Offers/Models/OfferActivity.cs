using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Offers.Models
{
    [Table("offer_activities")]
    public class OfferActivity
    {
        [Key]
        [Column("id")]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("offer_id")]
        [MaxLength(50)]
        public string OfferId { get; set; } = string.Empty;

        // Activity Information
        [Required]
        [Column("type")]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("details")]
        public string? Details { get; set; }

        // Old/New Values for tracking
        [Column("old_value")]
        [MaxLength(255)]
        public string? OldValue { get; set; }

        [Column("new_value")]
        [MaxLength(255)]
        public string? NewValue { get; set; }

        // Timestamps & User
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("created_by")]
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        [Column("created_by_name")]
        [MaxLength(255)]
        public string CreatedByName { get; set; } = string.Empty;

        // Navigation Property
        [ForeignKey("OfferId")]
        public virtual Offer? Offer { get; set; }
    }
}
