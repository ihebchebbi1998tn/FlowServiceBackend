using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.EmailAccounts.Models
{
    public class CustomEmailAccount
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public int? UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DisplayName { get; set; }

        [MaxLength(50)]
        public string Provider { get; set; } = "custom";

        // SMTP
        [MaxLength(255)]
        public string? SmtpServer { get; set; }
        public int? SmtpPort { get; set; }
        [MaxLength(20)]
        public string? SmtpSecurity { get; set; }

        // IMAP
        [MaxLength(255)]
        public string? ImapServer { get; set; }
        public int? ImapPort { get; set; }
        [MaxLength(20)]
        public string? ImapSecurity { get; set; }

        // POP3
        [MaxLength(255)]
        public string? Pop3Server { get; set; }
        public int? Pop3Port { get; set; }
        [MaxLength(20)]
        public string? Pop3Security { get; set; }

        // Encrypted credentials
        public string? EncryptedPassword { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastSyncedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
