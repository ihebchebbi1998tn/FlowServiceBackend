using System.Text.Json.Serialization;

namespace MyApi.Modules.Users.DTOs
{
    /// <summary>
    /// Dedicated DTO for updating a regular user's profile picture URL.
    /// </summary>
    public class UpdateProfilePictureRequestDto
    {
        [JsonPropertyName("profilePictureUrl")]
        public string? ProfilePictureUrl { get; set; }
    }
}
