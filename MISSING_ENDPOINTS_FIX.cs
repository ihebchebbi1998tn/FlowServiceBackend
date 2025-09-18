// =====================================================================
// MISSING ENDPOINTS IDENTIFIED IN LOOKUPS CONTROLLER
// =====================================================================
// The following endpoints are defined in ILookupService interface but 
// missing from LookupsController.cs

// Add these endpoints to LookupsController.cs:

// 1. Individual GET endpoints for some lookup types
[HttpGet("article-statuses/{id}")]
public async Task<ActionResult<LookupItemDto>> GetArticleStatus(string id)
{
    try
    {
        var result = await _lookupService.GetArticleStatusByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving article status with ID: {Id}", id);
        return StatusCode(500, "An error occurred while retrieving the article status.");
    }
}

// 2. UPDATE endpoints for article statuses
[HttpPut("article-statuses/{id}")]
public async Task<ActionResult<LookupItemDto>> UpdateArticleStatus(string id, [FromBody] UpdateLookupItemRequestDto updateDto)
{
    try
    {
        var result = await _lookupService.UpdateArticleStatusAsync(id, updateDto, GetCurrentUser());
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating article status with ID: {Id}", id);
        return StatusCode(500, "An error occurred while updating the article status.");
    }
}

// 3. DELETE endpoints for article statuses
[HttpDelete("article-statuses/{id}")]
public async Task<IActionResult> DeleteArticleStatus(string id)
{
    try
    {
        var result = await _lookupService.DeleteArticleStatusAsync(id, GetCurrentUser());
        if (!result)
            return NotFound();
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting article status with ID: {Id}", id);
        return StatusCode(500, "An error occurred while deleting the article status.");
    }
}

// Similar patterns needed for ALL lookup types:
// - service-categories (missing GET by ID, PUT, DELETE)
// - task-statuses (missing GET by ID, PUT, DELETE)
// - event-types (missing GET by ID, PUT, DELETE)
// - priorities (missing GET by ID, PUT, DELETE)
// - technician-statuses (missing GET by ID, PUT, DELETE)
// - leave-types (missing GET by ID, PUT, DELETE)
// - project-statuses (missing GET by ID, PUT, DELETE)
// - project-types (missing GET by ID, PUT, DELETE)
// - offer-statuses (missing GET by ID, PUT, DELETE)
// - skills (missing GET by ID, PUT, DELETE)
// - countries (missing GET by ID, PUT, DELETE)
// - currencies (missing GET by ID, PUT, DELETE)

// =====================================================================
// PATTERN FOR ALL MISSING ENDPOINTS
// =====================================================================

// For each lookup type, add these 3 endpoints:

// GET by ID
[HttpGet("{type}s/{id}")]
public async Task<ActionResult<LookupItemDto>> Get{Type}(string id)
{
    try
    {
        var result = await _lookupService.Get{Type}ByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving {type} with ID: {Id}", id);
        return StatusCode(500, "An error occurred while retrieving the {type}.");
    }
}

// UPDATE by ID
[HttpPut("{type}s/{id}")]
public async Task<ActionResult<LookupItemDto>> Update{Type}(string id, [FromBody] UpdateLookupItemRequestDto updateDto)
{
    try
    {
        var result = await _lookupService.Update{Type}Async(id, updateDto, GetCurrentUser());
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating {type} with ID: {Id}", id);
        return StatusCode(500, "An error occurred while updating the {type}.");
    }
}

// DELETE by ID
[HttpDelete("{type}s/{id}")]
public async Task<IActionResult> Delete{Type}(string id)
{
    try
    {
        var result = await _lookupService.Delete{Type}Async(id, GetCurrentUser());
        if (!result)
            return NotFound();
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting {type} with ID: {Id}", id);
        return StatusCode(500, "An error occurred while deleting the {type}.");
    }
}

// =====================================================================
// CURRENCY ENDPOINTS MISSING
// =====================================================================

// GET currency by ID
[HttpGet("currencies/{id}")]
public async Task<ActionResult<CurrencyDto>> GetCurrency(string id)
{
    try
    {
        var result = await _lookupService.GetCurrencyByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving currency with ID: {Id}", id);
        return StatusCode(500, "An error occurred while retrieving the currency.");
    }
}

// UPDATE currency
[HttpPut("currencies/{id}")]
public async Task<ActionResult<CurrencyDto>> UpdateCurrency(string id, [FromBody] UpdateCurrencyRequestDto updateDto)
{
    try
    {
        var result = await _lookupService.UpdateCurrencyAsync(id, updateDto, GetCurrentUser());
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating currency with ID: {Id}", id);
        return StatusCode(500, "An error occurred while updating the currency.");
    }
}

// DELETE currency
[HttpDelete("currencies/{id}")]
public async Task<IActionResult> DeleteCurrency(string id)
{
    try
    {
        var result = await _lookupService.DeleteCurrencyAsync(id, GetCurrentUser());
        if (!result)
            return NotFound();
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting currency with ID: {Id}", id);
        return StatusCode(500, "An error occurred while deleting the currency.");
    }
}