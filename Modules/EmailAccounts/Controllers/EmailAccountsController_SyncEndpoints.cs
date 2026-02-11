using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.EmailAccounts.DTOs;

namespace MyApi.Modules.EmailAccounts.Controllers
{
    public partial class EmailAccountsController
    {
// ─── Email Sync ───

/// <summary>
/// Trigger email sync from provider (Gmail/Outlook). Fetches latest emails.
/// </summary>
[HttpPost("{id}/sync-emails")]
public async Task<ActionResult<SyncResultDto>> SyncEmails(Guid id, [FromQuery] int maxResults = 50)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    try
    {
        var result = await _emailAccountService.SyncEmailsAsync(id, userId, maxResults);
        return Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "Email sync failed for account {Id}", id);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during email sync for account {Id}", id);
        return StatusCode(500, new { message = "Failed to sync emails" });
    }
}

/// <summary>
/// Get synced emails for a connected account with pagination and search
/// </summary>
[HttpGet("{id}/emails")]
public async Task<ActionResult<SyncedEmailsPageDto>> GetSyncedEmails(
    Guid id,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 25,
    [FromQuery] string? search = null)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    var result = await _emailAccountService.GetSyncedEmailsAsync(id, userId, page, pageSize, search);
    return Ok(result);
}

// ─── Calendar Sync ───

/// <summary>
/// Trigger calendar sync from provider (Google Calendar / Outlook Calendar).
/// </summary>
[HttpPost("{id}/sync-calendar")]
public async Task<ActionResult<CalendarSyncResultDto>> SyncCalendar(Guid id, [FromQuery] int maxResults = 50)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    try
    {
        var result = await _emailAccountService.SyncCalendarAsync(id, userId, maxResults);
        return Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "Calendar sync failed for account {Id}", id);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during calendar sync for account {Id}", id);
        return StatusCode(500, new { message = "Failed to sync calendar" });
    }
}

/// <summary>
/// Get synced calendar events for a connected account with pagination and search
/// </summary>
[HttpGet("{id}/calendar-events")]
public async Task<ActionResult<SyncedCalendarEventsPageDto>> GetCalendarEvents(
    Guid id,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 25,
    [FromQuery] string? search = null)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    var result = await _emailAccountService.GetCalendarEventsAsync(id, userId, page, pageSize, search);
    return Ok(result);
}

// ─── Send Email ───

/// <summary>
/// Send an email through a connected account (Gmail or Outlook)
/// </summary>
[HttpPost("{id}/send-email")]
public async Task<ActionResult<SendEmailResultDto>> SendEmail(Guid id, [FromBody] SendEmailDto dto)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    try
    {
        var result = await _emailAccountService.SendEmailAsync(id, userId, dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send email for account {Id}", id);
        return StatusCode(500, new { message = "Failed to send email" });
    }
}

// ─── Star / Unstar Email ───

/// <summary>
/// Toggle the starred/favorite status of a synced email (updates both local DB and provider).
/// </summary>
[HttpPatch("{id}/emails/{emailId}/star")]
[ProducesResponseType(typeof(object), 200)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
public async Task<IActionResult> ToggleStarEmail(Guid id, Guid emailId)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    try
    {
        var result = await _emailAccountService.ToggleStarEmailAsync(id, userId, emailId);
        if (!result) return NotFound(new { message = "Email not found" });
        return Ok(new { success = true });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to toggle star for email {EmailId} in account {Id}", emailId, id);
        return StatusCode(500, new { message = "Failed to toggle star" });
    }
}

// ─── Mark as Read / Unread ───

/// <summary>
/// Toggle the read/unread status of a synced email (updates both local DB and provider).
/// </summary>
[HttpPatch("{id}/emails/{emailId}/read")]
[ProducesResponseType(typeof(object), 200)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
public async Task<IActionResult> ToggleReadEmail(Guid id, Guid emailId)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    try
    {
        var result = await _emailAccountService.ToggleReadEmailAsync(id, userId, emailId);
        if (!result) return NotFound(new { message = "Email not found" });
        return Ok(new { success = true });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to toggle read for email {EmailId} in account {Id}", emailId, id);
        return StatusCode(500, new { message = "Failed to toggle read status" });
    }
}

// ─── Delete Email ───

/// <summary>
/// Delete/trash a synced email (moves to trash on provider, removes from local DB).
/// </summary>
[HttpDelete("{id}/emails/{emailId}")]
[ProducesResponseType(204)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
public async Task<IActionResult> DeleteEmail(Guid id, Guid emailId)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    try
    {
        var result = await _emailAccountService.DeleteEmailAsync(id, userId, emailId);
        if (!result) return NotFound(new { message = "Email not found" });
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete email {EmailId} in account {Id}", emailId, id);
        return StatusCode(500, new { message = "Failed to delete email" });
    }
}
    }
}
