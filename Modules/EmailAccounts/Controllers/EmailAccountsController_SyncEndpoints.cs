// ────────────────────────────────────────────────
// Email & Calendar Sync Endpoints
// ADD these endpoints INSIDE your existing EmailAccountsController class
// ────────────────────────────────────────────────

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
