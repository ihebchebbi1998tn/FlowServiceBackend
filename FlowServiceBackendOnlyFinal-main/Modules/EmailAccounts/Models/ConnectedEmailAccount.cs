using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.EmailAccounts.Models
{
    /// <summary>
    /// Represents a connected email/calendar account (Gmail or Outlook)
    /// </summary>
    public class ConnectedEmailAccount
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user who owns this connected account
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Email address / handle of the connected account
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Handle { get; set; } = string.Empty;

        /// <summary>
        /// Provider: "google" or "microsoft"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// OAuth access token (encrypted at rest recommended)
        /// </summary>
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// OAuth refresh token (encrypted at rest recommended)
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of granted OAuth scopes
        /// </summary>
        public string? Scopes { get; set; }

        /// <summary>
        /// Current sync status: not_synced, syncing, active, failed
        /// </summary>
        [MaxLength(50)]
        public string SyncStatus { get; set; } = "not_synced";

        /// <summary>
        /// When the last successful sync completed
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// If auth failed, when it happened (null = auth is fine)
        /// </summary>
        public DateTime? AuthFailedAt { get; set; }

        /// <summary>
        /// Email visibility setting: share_everything, subject, metadata
        /// </summary>
        [MaxLength(50)]
        public string EmailVisibility { get; set; } = "share_everything";

        /// <summary>
        /// Calendar visibility setting: share_everything, metadata
        /// </summary>
        [MaxLength(50)]
        public string CalendarVisibility { get; set; } = "share_everything";

        /// <summary>
        /// Contact auto creation policy: sent_and_received, sent, none
        /// </summary>
        [MaxLength(50)]
        public string ContactAutoCreationPolicy { get; set; } = "sent_and_received";

        /// <summary>
        /// Whether email sync is enabled
        /// </summary>
        public bool IsEmailSyncEnabled { get; set; } = true;

        /// <summary>
        /// Whether calendar sync is enabled
        /// </summary>
        public bool IsCalendarSyncEnabled { get; set; } = true;

        /// <summary>
        /// Whether to exclude group emails from sync
        /// </summary>
        public bool ExcludeGroupEmails { get; set; } = false;

        /// <summary>
        /// Whether to exclude non-professional emails
        /// </summary>
        public bool ExcludeNonProfessionalEmails { get; set; } = false;

        /// <summary>
        /// Whether contact auto creation is enabled for calendar
        /// </summary>
        public bool IsCalendarContactAutoCreationEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<EmailBlocklistItem> BlocklistItems { get; set; } = new List<EmailBlocklistItem>();
    }
}
