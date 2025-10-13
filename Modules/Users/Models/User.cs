using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Users.Models
{
    [Table("Users")]
    public class User
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
        public string CreatedUser { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ModifyUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifyDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        // Future: Role relationships will be added here
        [MaxLength(50)]
        public string? Role { get; set; } // Temporary until role system is implemented

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "character varying(500)")]
        public string? AccessToken { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "character varying(500)")]
        public string? RefreshToken { get; set; }

        public DateTime? TokenExpiresAt { get; set; }
    }
}
