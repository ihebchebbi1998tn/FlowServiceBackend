using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.EmailAccounts.Models
{
    /// <summary>
    /// A synced email message from Gmail or Outlook
    /// </summary>
    public class SyncedEmail
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The connected account this email belongs to
        /// </summary>
        public Guid ConnectedEmailAccountId { get; set; }

        [ForeignKey(nameof(ConnectedEmailAccountId))]
        public ConnectedEmailAccount? ConnectedEmailAccount { get; set; }

        /// <summary>
        /// External message ID (Gmail message ID or Outlook message ID)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Thread/conversation ID
        /// </summary>
        [MaxLength(255)]
        public string? ThreadId { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Short preview/snippet text
        /// </summary>
        public string? Snippet { get; set; }

        /// <summary>
        /// Sender email address
        /// </summary>
        [MaxLength(255)]
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// Sender display name
        /// </summary>
        [MaxLength(255)]
        public string? FromName { get; set; }

        /// <summary>
        /// JSON array of recipient emails
        /// </summary>
        public string? ToEmails { get; set; }

        /// <summary>
        /// JSON array of CC emails
        /// </summary>
        public string? CcEmails { get; set; }

        /// <summary>
        /// JSON array of BCC emails
        /// </summary>
        public string? BccEmails { get; set; }

        /// <summary>
        /// First ~200 chars of body
        /// </summary>
        public string? BodyPreview { get; set; }

        public bool IsRead { get; set; } = false;

        public bool IsStarred { get; set; } = false;

        public bool HasAttachments { get; set; } = false;

        /// <summary>
        /// JSON array of labels/folders
        /// </summary>
        public string? Labels { get; set; }

        /// <summary>
        /// When the email was received
        /// </summary>
        public DateTime ReceivedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
