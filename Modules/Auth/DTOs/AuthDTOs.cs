using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Auth.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class SignupRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

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

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(500)]
        public string? CompanyWebsite { get; set; }

        public string? Preferences { get; set; } // JSON string
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? Preferences { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool OnboardingCompleted { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UpdateUserRequestDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(2)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? Industry { get; set; }

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(500)]
        public string? CompanyWebsite { get; set; }

        public string? Preferences { get; set; }
        
        public bool? OnboardingCompleted { get; set; }
    }

    public class OAuthLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
