using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.SupportTickets.Models
{
    public class SupportTicket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Urgency { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? CurrentPage { get; set; }

        [StringLength(1000)]
        public string? RelatedUrl { get; set; }

        [Required]
        [StringLength(100)]
        public string Tenant { get; set; } = string.Empty;

        [StringLength(255)]
        public string? UserEmail { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SupportTicketAttachment> Attachments { get; set; } = new List<SupportTicketAttachment>();
    }

    public class SupportTicketAttachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SupportTicketId { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? FilePath { get; set; }

        public long FileSize { get; set; }

        [StringLength(200)]
        public string? ContentType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("SupportTicketId")]
        public virtual SupportTicket Ticket { get; set; } = null!;
    }
}
