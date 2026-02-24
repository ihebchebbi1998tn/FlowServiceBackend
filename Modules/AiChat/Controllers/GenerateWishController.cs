using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.AiChat.DTOs;
using MyApi.Modules.AiChat.Services;
using System.Security.Claims;

namespace MyApi.Modules.AiChat.Controllers
{
    /// <summary>
    /// Endpoint to generate AI responses via the local Ollama LLM.
    /// POST /api/GenerateWish — non-streaming JSON response
    /// POST /api/GenerateWish/stream — SSE streaming response
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
        /// Generate AI response (non-streaming). Accepts prompt or messages[].
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(GenerateWishResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [ProducesResponseType(503)]
        public async Task<ActionResult<GenerateWishResponseDto>> Generate([FromBody] GenerateWishRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Validate: at least prompt or messages must be provided
            if (string.IsNullOrWhiteSpace(dto.Prompt) && (dto.Messages == null || dto.Messages.Count == 0))
            {
                return BadRequest(new GenerateWishResponseDto
                {
                    Success = false,
                    Error = "Either 'prompt' or 'messages' must be provided"
                });
            }

            try
            {
                var userId = GetCurrentUserId();
                var result = await _ollamaService.GenerateAsync(dto, userId);

                if (!result.Success)
                {
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

        /// <summary>
        /// Stream AI response via SSE. Accepts prompt or messages[].
        /// </summary>
        [HttpPost("stream")]
        public async Task Stream([FromBody] GenerateWishRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Prompt) && (dto.Messages == null || dto.Messages.Count == 0))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("{\"error\":\"Either 'prompt' or 'messages' must be provided\"}");
                return;
            }

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var userId = GetCurrentUserId();
            await _ollamaService.StreamChatAsync(dto, userId, Response.Body, HttpContext.RequestAborted);
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
