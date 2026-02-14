using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.EmailAccounts.Models
{
    /// <summary>
    /// Blocked email address or domain for a connected account
    /// </summary>
    public class EmailBlocklistItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The connected account this blocklist entry belongs to
        /// </summary>
        public Guid ConnectedEmailAccountId { get; set; }

        [ForeignKey(nameof(ConnectedEmailAccountId))]
        public ConnectedEmailAccount? ConnectedEmailAccount { get; set; }

        /// <summary>
        /// Blocked email or @domain pattern (e.g. "spam@test.com" or "@spam.com")
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Handle { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
