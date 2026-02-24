using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.AiChat.DTOs
{
    // ── Request ──
    public class GenerateWishRequestDto
    {
        [Required(ErrorMessage = "Prompt is required")]
        [MinLength(1, ErrorMessage = "Prompt cannot be empty")]
        [MaxLength(4000, ErrorMessage = "Prompt cannot exceed 4000 characters")]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Ollama model name. Defaults to "llama3:8b".
        /// </summary>
        [MaxLength(100)]
        public string? Model { get; set; }

        /// <summary>
        /// Optional conversation ID to persist the exchange.
        /// </summary>
        public int? ConversationId { get; set; }
    }

    // ── Response ──
    public class GenerateWishResponseDto
    {
        public bool Success { get; set; }
        public string Response { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public long TotalDurationNs { get; set; }
        public int? ConversationId { get; set; }
        public string? Error { get; set; }
    }

    // ── Ollama internal payloads ──
    internal class OllamaGenerateRequest
    {
        public string Model { get; set; } = "llama3:8b";
        public string Prompt { get; set; } = string.Empty;
        public bool Stream { get; set; } = false;
    }

    internal class OllamaGenerateResponse
    {
        public string Model { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
        public long Total_duration { get; set; }
    }
}
