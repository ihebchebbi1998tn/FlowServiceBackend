using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.AiChat.DTOs;
using MyApi.Modules.AiChat.Services;
using System.Security.Claims;

namespace MyApi.Modules.AiChat.Controllers
{
    /// <summary>
    /// Endpoint to generate AI responses via the local Ollama LLM.
    /// POST /api/GenerateWish
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GenerateWishController : ControllerBase
    {
        private readonly IOllamaService _ollamaService;
        private readonly ILogger<GenerateWishController> _logger;

        public GenerateWishController(IOllamaService ollamaService, ILogger<GenerateWishController> logger)
        {
            _ollamaService = ollamaService;
            _logger = logger;
        }

        /// <summary>
        /// Generate AI response from a user prompt using local Ollama LLM.
        /// </summary>
        /// <param name="dto">The prompt and optional model/conversation info</param>
        /// <returns>AI-generated response</returns>
        [HttpPost]
        [ProducesResponseType(typeof(GenerateWishResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [ProducesResponseType(503)]
        public async Task<ActionResult<GenerateWishResponseDto>> Generate([FromBody] GenerateWishRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var result = await _ollamaService.GenerateAsync(dto, userId);

                if (!result.Success)
                {
                    // Distinguish between "LLM unreachable" (503) and "LLM error" (502)
                    if (result.Error?.Contains("Cannot reach") == true ||
                        result.Error?.Contains("timed out") == true)
                    {
                        return StatusCode(503, result);
                    }
                    return StatusCode(502, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GenerateWish");
                return StatusCode(500, new GenerateWishResponseDto
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                   User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("sub")?.Value ??
                   "anonymous";
        }
    }
}
