using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.EmailAccounts.Models
{
    /// <summary>
    /// A synced calendar event from Google Calendar or Outlook Calendar
    /// </summary>
    public class SyncedCalendarEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The connected account this event belongs to
        /// </summary>
        public Guid ConnectedEmailAccountId { get; set; }

        [ForeignKey(nameof(ConnectedEmailAccountId))]
        public ConnectedEmailAccount? ConnectedEmailAccount { get; set; }

        /// <summary>
        /// External event ID (Google Calendar event ID or Outlook event ID)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Event title/summary
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Event description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Event location
        /// </summary>
        [MaxLength(500)]
        public string? Location { get; set; }

        /// <summary>
        /// When the event starts
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// When the event ends
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Whether this is an all-day event
        /// </summary>
        public bool IsAllDay { get; set; } = false;

        /// <summary>
        /// Event status: confirmed, tentative, cancelled
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "confirmed";

        /// <summary>
        /// Organizer email address
        /// </summary>
        [MaxLength(255)]
        public string? OrganizerEmail { get; set; }

        /// <summary>
        /// JSON array of attendee email addresses
        /// </summary>
        public string? Attendees { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
