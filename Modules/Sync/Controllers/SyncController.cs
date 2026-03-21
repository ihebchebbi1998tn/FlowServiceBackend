using System;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.Sync.DTOs;
using MyApi.Modules.Sync.Services;

namespace MyApi.Modules.Sync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;

        public SyncController(ISyncService syncService)
        {
            _syncService = syncService;
        }

        [HttpPost("push")]
        public async Task<ActionResult<SyncPushResponseDto>> Push([FromBody] SyncPushRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.Operations == null || request.Operations.Count == 0) return Ok(new SyncPushResponseDto());
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized("User identity claim is required");
            var result = await _syncService.PushAsync(request, currentUser);
            return Ok(result);
        }

        [HttpGet("pull")]
        public async Task<ActionResult<SyncPullResponseDto>> Pull([FromQuery] string? cursor = null, [FromQuery] int limit = 200)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null) return Unauthorized("User identity claim is required");
                var result = await _syncService.PullAsync(cursor, limit, currentUser, IsAdmin());
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<SyncHistoryResponseDto>> History([FromQuery] SyncHistoryQueryDto query)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized("User identity claim is required");
            var result = await _syncService.GetHistoryAsync(query, currentUser, IsAdmin());
            return Ok(result);
        }

        [HttpPost("retry")]
        public async Task<ActionResult<SyncPushResultDto>> Retry([FromBody] SyncRetryRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.OpId))
                return BadRequest("deviceId and opId are required");
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null) return Unauthorized("User identity claim is required");
                var result = await _syncService.RetryAsync(request, currentUser, IsAdmin());
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string? GetCurrentUser()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("admin")
                || User.IsInRole("Admin")
                || string.Equals(User.FindFirst(ClaimTypes.Role)?.Value, "admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(User.FindFirst("role")?.Value, "admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
