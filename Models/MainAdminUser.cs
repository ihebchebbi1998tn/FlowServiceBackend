
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models
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

        [Required(AllowEmptyStrings = true)]
        [MaxLength(100)]
        public string Industry { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column(TypeName = "character varying(500)")]
        public string? AccessToken { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "character varying(500)")]
        public string? RefreshToken { get; set; }

        public DateTime? TokenExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        // Navigation properties for future use
        [MaxLength(255)]
        public string? CompanyName { get; set; }
        
        [MaxLength(500)]
        public string? CompanyWebsite { get; set; }
        
        [Column(TypeName = "text")]
        public string? Preferences { get; set; } // JSON string for user preferences
    }
}
