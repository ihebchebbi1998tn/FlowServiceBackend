using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.Lookups.DTOs;
using MyApi.Modules.Lookups.Services;

namespace MyApi.Modules.Lookups.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LookupsController : ControllerBase
    {
        private readonly ILookupService _lookupService;
        private readonly ILogger<LookupsController> _logger;

        public LookupsController(ILookupService lookupService, ILogger<LookupsController> logger)
        {
            _lookupService = lookupService;
            _logger = logger;
        }

        // Article Categories
        [HttpGet("article-categories")]
        public async Task<ActionResult<LookupListResponseDto>> GetArticleCategories()
        {
            try
            {
                var result = await _lookupService.GetArticleCategoriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving article categories");
                return StatusCode(500, "An error occurred while retrieving article categories.");
            }
        }

        [HttpGet("article-categories/{id}")]
        public async Task<ActionResult<LookupItemDto>> GetArticleCategory(string id)
        {
            try
            {
                var result = await _lookupService.GetArticleCategoryByIdAsync(id);
                if (result == null)
                    return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving article category with ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the article category.");
            }
        }

        [HttpPost("article-categories")]
        public async Task<ActionResult<LookupItemDto>> CreateArticleCategory([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateArticleCategoryAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetArticleCategory), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating article category");
                return StatusCode(500, "An error occurred while creating the article category.");
            }
        }

        [HttpPut("article-categories/{id}")]
        public async Task<ActionResult<LookupItemDto>> UpdateArticleCategory(string id, [FromBody] UpdateLookupItemRequestDto updateDto)
        {
            try
            {
                var result = await _lookupService.UpdateArticleCategoryAsync(id, updateDto, GetCurrentUser());
                if (result == null)
                    return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating article category with ID: {Id}", id);
                return StatusCode(500, "An error occurred while updating the article category.");
            }
        }

        [HttpDelete("article-categories/{id}")]
        public async Task<IActionResult> DeleteArticleCategory(string id)
        {
            try
            {
                var result = await _lookupService.DeleteArticleCategoryAsync(id, GetCurrentUser());
                if (!result)
                    return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting article category with ID: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the article category.");
            }
        }

        // Article Statuses
        [HttpGet("article-statuses")]
        public async Task<ActionResult<LookupListResponseDto>> GetArticleStatuses()
        {
            try
            {
                var result = await _lookupService.GetArticleStatusesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving article statuses");
                return StatusCode(500, "An error occurred while retrieving article statuses.");
            }
        }

        [HttpPost("article-statuses")]
        public async Task<ActionResult<LookupItemDto>> CreateArticleStatus([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateArticleStatusAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetArticleStatuses), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating article status");
                return StatusCode(500, "An error occurred while creating the article status.");
            }
        }

        // Service Categories
        [HttpGet("service-categories")]
        public async Task<ActionResult<LookupListResponseDto>> GetServiceCategories()
        {
            try
            {
                var result = await _lookupService.GetServiceCategoriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service categories");
                return StatusCode(500, "An error occurred while retrieving service categories.");
            }
        }

        [HttpPost("service-categories")]
        public async Task<ActionResult<LookupItemDto>> CreateServiceCategory([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateServiceCategoryAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetServiceCategories), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service category");
                return StatusCode(500, "An error occurred while creating the service category.");
            }
        }

        // Task Statuses
        [HttpGet("task-statuses")]
        public async Task<ActionResult<LookupListResponseDto>> GetTaskStatuses()
        {
            try
            {
                var result = await _lookupService.GetTaskStatusesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task statuses");
                return StatusCode(500, "An error occurred while retrieving task statuses.");
            }
        }

        [HttpPost("task-statuses")]
        public async Task<ActionResult<LookupItemDto>> CreateTaskStatus([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateTaskStatusAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetTaskStatuses), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task status");
                return StatusCode(500, "An error occurred while creating the task status.");
            }
        }

        // Event Types
        [HttpGet("event-types")]
        public async Task<ActionResult<LookupListResponseDto>> GetEventTypes()
        {
            try
            {
                var result = await _lookupService.GetEventTypesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event types");
                return StatusCode(500, "An error occurred while retrieving event types.");
            }
        }

        [HttpPost("event-types")]
        public async Task<ActionResult<LookupItemDto>> CreateEventType([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateEventTypeAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetEventTypes), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event type");
                return StatusCode(500, "An error occurred while creating the event type.");
            }
        }

        // Priorities
        [HttpGet("priorities")]
        public async Task<ActionResult<LookupListResponseDto>> GetPriorities()
        {
            try
            {
                var result = await _lookupService.GetPrioritiesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving priorities");
                return StatusCode(500, "An error occurred while retrieving priorities.");
            }
        }

        [HttpPost("priorities")]
        public async Task<ActionResult<LookupItemDto>> CreatePriority([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreatePriorityAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetPriorities), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating priority");
                return StatusCode(500, "An error occurred while creating the priority.");
            }
        }

        // Technician Statuses
        [HttpGet("technician-statuses")]
        public async Task<ActionResult<LookupListResponseDto>> GetTechnicianStatuses()
        {
            try
            {
                var result = await _lookupService.GetTechnicianStatusesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving technician statuses");
                return StatusCode(500, "An error occurred while retrieving technician statuses.");
            }
        }

        [HttpPost("technician-statuses")]
        public async Task<ActionResult<LookupItemDto>> CreateTechnicianStatus([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateTechnicianStatusAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetTechnicianStatuses), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating technician status");
                return StatusCode(500, "An error occurred while creating the technician status.");
            }
        }

        // Leave Types
        [HttpGet("leave-types")]
        public async Task<ActionResult<LookupListResponseDto>> GetLeaveTypes()
        {
            try
            {
                var result = await _lookupService.GetLeaveTypesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave types");
                return StatusCode(500, "An error occurred while retrieving leave types.");
            }
        }

        [HttpPost("leave-types")]
        public async Task<ActionResult<LookupItemDto>> CreateLeaveType([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateLeaveTypeAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetLeaveTypes), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave type");
                return StatusCode(500, "An error occurred while creating the leave type.");
            }
        }

        // Project Statuses
        [HttpGet("project-statuses")]
        public async Task<ActionResult<LookupListResponseDto>> GetProjectStatuses()
        {
            try
            {
                var result = await _lookupService.GetProjectStatusesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project statuses");
                return StatusCode(500, "An error occurred while retrieving project statuses.");
            }
        }

        [HttpPost("project-statuses")]
        public async Task<ActionResult<LookupItemDto>> CreateProjectStatus([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateProjectStatusAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetProjectStatuses), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project status");
                return StatusCode(500, "An error occurred while creating the project status.");
            }
        }

        // Project Types
        [HttpGet("project-types")]
        public async Task<ActionResult<LookupListResponseDto>> GetProjectTypes()
        {
            try
            {
                var result = await _lookupService.GetProjectTypesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project types");
                return StatusCode(500, "An error occurred while retrieving project types.");
            }
        }

        [HttpPost("project-types")]
        public async Task<ActionResult<LookupItemDto>> CreateProjectType([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateProjectTypeAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetProjectTypes), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project type");
                return StatusCode(500, "An error occurred while creating the project type.");
            }
        }

        // Offer Statuses
        [HttpGet("offer-statuses")]
        public async Task<ActionResult<LookupListResponseDto>> GetOfferStatuses()
        {
            try
            {
                var result = await _lookupService.GetOfferStatusesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving offer statuses");
                return StatusCode(500, "An error occurred while retrieving offer statuses.");
            }
        }

        [HttpPost("offer-statuses")]
        public async Task<ActionResult<LookupItemDto>> CreateOfferStatus([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateOfferStatusAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetOfferStatuses), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating offer status");
                return StatusCode(500, "An error occurred while creating the offer status.");
            }
        }

        // Skills
        [HttpGet("skills")]
        public async Task<ActionResult<LookupListResponseDto>> GetSkills()
        {
            try
            {
                var result = await _lookupService.GetSkillsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving skills");
                return StatusCode(500, "An error occurred while retrieving skills.");
            }
        }

        [HttpPost("skills")]
        public async Task<ActionResult<LookupItemDto>> CreateSkill([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateSkillAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetSkills), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating skill");
                return StatusCode(500, "An error occurred while creating the skill.");
            }
        }

        // Countries
        [HttpGet("countries")]
        public async Task<ActionResult<LookupListResponseDto>> GetCountries()
        {
            try
            {
                var result = await _lookupService.GetCountriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving countries");
                return StatusCode(500, "An error occurred while retrieving countries.");
            }
        }

        [HttpPost("countries")]
        public async Task<ActionResult<LookupItemDto>> CreateCountry([FromBody] CreateLookupItemRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateCountryAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetCountries), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating country");
                return StatusCode(500, "An error occurred while creating the country.");
            }
        }

        // Currencies
        [HttpGet("currencies")]
        public async Task<ActionResult<CurrencyListResponseDto>> GetCurrencies()
        {
            try
            {
                var result = await _lookupService.GetCurrenciesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currencies");
                return StatusCode(500, "An error occurred while retrieving currencies.");
            }
        }

        [HttpPost("currencies")]
        public async Task<ActionResult<CurrencyDto>> CreateCurrency([FromBody] CreateCurrencyRequestDto createDto)
        {
            try
            {
                var result = await _lookupService.CreateCurrencyAsync(createDto, GetCurrentUser());
                return CreatedAtAction(nameof(GetCurrencies), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating currency");
                return StatusCode(500, "An error occurred while creating the currency.");
            }
        }

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

        private string GetCurrentUser()
        {
            return User?.Identity?.Name ?? User?.FindFirst("email")?.Value ?? "system";
        }
    }
}
