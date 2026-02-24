using MyApi.Modules.AiChat.DTOs;

namespace MyApi.Modules.AiChat.Services
{
    public interface IOllamaService
    {
        /// <summary>
        /// Sends a prompt to the local Ollama LLM and returns the response.
        /// </summary>
        Task<GenerateWishResponseDto> GenerateAsync(GenerateWishRequestDto request, string userId);
    }
}
