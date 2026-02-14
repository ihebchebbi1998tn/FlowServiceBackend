using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.EmailAccounts.Models
{
    /// <summary>
    /// Stores metadata for attachments on synced emails.
    /// Actual content is fetched on-demand from the provider (Gmail/Outlook).
    /// </summary>
    public class SyncedEmailAttachment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The synced email this attachment belongs to
        /// </summary>
        public Guid SyncedEmailId { get; set; }

        [ForeignKey(nameof(SyncedEmailId))]
        public SyncedEmail? SyncedEmail { get; set; }

        /// <summary>
        /// Provider-specific attachment ID (Gmail attachmentId or Outlook attachment id)
        /// Used to fetch the actual content on demand
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string ExternalAttachmentId { get; set; } = string.Empty;

        /// <summary>
        /// Original file name of the attachment
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type (e.g., application/pdf, image/png)
        /// </summary>
        [MaxLength(100)]
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
