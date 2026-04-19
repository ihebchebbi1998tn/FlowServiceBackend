using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.ExternalEndpoints.DTOs;
using MyApi.Modules.ExternalEndpoints.Services;
using MyApi.Modules.Shared.Services;
using System.Security.Claims;

namespace MyApi.Modules.ExternalEndpoints.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/external-endpoints")]
    public class ExternalEndpointsController : ControllerBase
    {
        private readonly IExternalEndpointService _service;
        private readonly ISystemLogService _systemLogService;
        private readonly ILogger<ExternalEndpointsController> _logger;

        public ExternalEndpointsController(IExternalEndpointService service, ISystemLogService systemLogService, ILogger<ExternalEndpointsController> logger)
        {
            _service = service;
            _systemLogService = systemLogService;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        private string GetUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "anonymous";

        [HttpGet]
        public async Task<IActionResult> GetEndpoints([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            try
            {
                var result = await _service.GetEndpointsAsync(search, status, page, limit);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching external endpoints");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to fetch endpoints" } });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _service.GetStatsAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching endpoint stats");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to fetch stats" } });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var endpoint = await _service.GetEndpointByIdAsync(id);
                if (endpoint == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Endpoint not found" } });
                return Ok(new { success = true, data = endpoint });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to fetch endpoint" } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExternalEndpointDto dto)
        {
            try
            {
                var userId = GetUserId();
                var endpoint = await _service.CreateEndpointAsync(dto, userId);
                await _systemLogService.LogSuccessAsync($"External endpoint created: {endpoint.Name}", "ExternalEndpoints", "create", userId, GetUserName(), "ExternalEndpoint", endpoint.Id.ToString());
                return CreatedAtAction(nameof(GetById), new { id = endpoint.Id }, new { success = true, data = endpoint });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating endpoint");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to create endpoint" } });
            }
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExternalEndpointDto dto)
        {
            try
            {
                var userId = GetUserId();
                var endpoint = await _service.UpdateEndpointAsync(id, dto, userId);
                await _systemLogService.LogSuccessAsync($"External endpoint updated: {endpoint.Name}", "ExternalEndpoints", "update", userId, GetUserName(), "ExternalEndpoint", id.ToString());
                return Ok(new { success = true, data = endpoint });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = ex.Message } });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Endpoint not found" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to update endpoint" } });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteEndpointAsync(id, GetUserId());
                if (!result) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Endpoint not found" } });
                await _systemLogService.LogSuccessAsync($"External endpoint deleted: {id}", "ExternalEndpoints", "delete", GetUserId(), GetUserName(), "ExternalEndpoint", id.ToString());
                return Ok(new { success = true, message = "Endpoint deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to delete endpoint" } });
            }
        }

        [HttpPost("{id:int}/regenerate-key")]
        public async Task<IActionResult> RegenerateKey(int id)
        {
            try
            {
                var endpoint = await _service.RegenerateKeyAsync(id, GetUserId());
                return Ok(new { success = true, data = endpoint });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Endpoint not found" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating key for endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to regenerate key" } });
            }
        }

        // Reveal the plain API key for an authenticated tenant user.
        // API keys are returned masked on list/get for security; this endpoint
        // is the only authenticated way to read the plain key after creation.
        [HttpGet("{id:int}/reveal-key")]
        public async Task<IActionResult> RevealKey(int id)
        {
            try
            {
                var apiKey = await _service.RevealKeyAsync(id);
                await _systemLogService.LogSuccessAsync($"External endpoint API key revealed: {id}", "ExternalEndpoints", "read", GetUserId(), GetUserName(), "ExternalEndpoint", id.ToString());
                return Ok(new { success = true, data = new { apiKey } });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Endpoint not found" } });
            }
            catch (InvalidOperationException ex)
            {
                // Expected when the stored key is hashed and cannot be reversed.
                // 409 Conflict + an explicit code lets the UI offer "Regenerate" as the recovery path.
                return Conflict(new { success = false, error = new { code = "KEY_HASHED", message = ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revealing key for endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to reveal key" } });
            }
        }

        [HttpGet("{id:int}/logs")]
        public async Task<IActionResult> GetLogs(int id, [FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            try
            {
                var result = await _service.GetLogsAsync(id, page, limit);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs for endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to fetch logs" } });
            }
        }

        [HttpGet("{id:int}/logs/{logId:int}")]
        public async Task<IActionResult> GetLog(int id, int logId)
        {
            try
            {
                var log = await _service.GetLogByIdAsync(id, logId);
                if (log == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Log not found" } });
                return Ok(new { success = true, data = log });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching log {LogId}", logId);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to fetch log" } });
            }
        }

        [HttpDelete("{id:int}/logs/{logId:int}")]
        public async Task<IActionResult> DeleteLog(int id, int logId)
        {
            try
            {
                var result = await _service.DeleteLogAsync(id, logId);
                if (!result) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Log not found" } });
                return Ok(new { success = true, message = "Log deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting log {LogId}", logId);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to delete log" } });
            }
        }

        [HttpDelete("{id:int}/logs")]
        public async Task<IActionResult> ClearLogs(int id)
        {
            try
            {
                await _service.ClearLogsAsync(id);
                return Ok(new { success = true, message = "All logs cleared" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing logs for endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to clear logs" } });
            }
        }

        [HttpPatch("{id:int}/logs/{logId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id, int logId)
        {
            try
            {
                var result = await _service.MarkLogAsReadAsync(id, logId);
                if (!result) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Log not found" } });
                return Ok(new { success = true, message = "Log marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking log {LogId} as read", logId);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to mark log" } });
            }
        }

        [HttpPost("{id:int}/test")]
        public async Task<IActionResult> TestEndpoint(int id, [FromBody] object testBody)
        {
            try
            {
                // GetEndpointByIdAsync returns the API key MASKED for security.
                // For the in-app Test button we need the plain key so ReceiveAsync's
                // auth check passes — fetch it via the dedicated reveal path.
                var endpoint = await _service.GetEndpointByIdAsync(id);
                if (endpoint == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Endpoint not found" } });

                var plainApiKey = await _service.RevealKeyAsync(id);

                var (statusCode, responseBody) = await _service.ReceiveAsync(
                    endpoint.Slug, "POST", null, null,
                    System.Text.Json.JsonSerializer.Serialize(testBody),
                    "127.0.0.1", plainApiKey
                );

                return Ok(new { success = true, data = new { statusCode, responseBody } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing endpoint {Id}", id);
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = "Failed to test endpoint" } });
            }
        }
    }
}
