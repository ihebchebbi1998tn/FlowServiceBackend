using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendApi.Models
{
    [Table("event_attendees")]
    public class EventAttendee
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        public string? Response { get; set; }

        [Column("responded_at")]
        public DateTime? RespondedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual CalendarEvent CalendarEvent { get; set; } = null!;
    }
}