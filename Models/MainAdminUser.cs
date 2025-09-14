using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowServiceBackend.Models
{
    [Table("MainAdminUsers")]
    public class MainAdminUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [MaxLength(2)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Industry { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? AccessToken { get; set; }

        [Column(TypeName = "text")]
        public string? RefreshToken { get; set; }

        public DateTime? TokenExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        // Navigation properties for future use
        public string? CompanyName { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? Preferences { get; set; } // JSON string for user preferences
    }
}
