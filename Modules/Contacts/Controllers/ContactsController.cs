using MyApi.Modules.Contacts.DTOs;
using MyApi.Modules.Contacts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MyApi.Modules.Contacts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _contactService;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(IContactService contactService, ILogger<ContactsController> logger)
        {
            _contactService = contactService;
            _logger = logger;
        }

        /// <summary>
        /// Get all contacts with optional filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ContactListResponseDto>> GetAllContacts([FromQuery] ContactSearchRequestDto? searchRequest = null)
        {
            try
            {
                var result = await _contactService.GetAllContactsAsync(searchRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contacts");
                return StatusCode(500, "An error occurred while retrieving contacts");
            }
        }

        /// <summary>
        /// Get contact by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContactResponseDto>> GetContact(int id)
        {
            try
            {
                var contact = await _contactService.GetContactByIdAsync(id);
                
                if (contact == null)
                {
                    return NotFound($"Contact with ID {id} not found");
                }

                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact with ID {ContactId}", id);
                return StatusCode(500, "An error occurred while retrieving the contact");
            }
        }

        /// <summary>
        /// Create a new contact
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ContactResponseDto>> CreateContact([FromBody] CreateContactRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var contact = await _contactService.CreateContactAsync(createDto, currentUser);

                return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating contact");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return StatusCode(500, "An error occurred while creating the contact");
            }
        }

        /// <summary>
        /// Update an existing contact
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ContactResponseDto>> UpdateContact(int id, [FromBody] UpdateContactRequestDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var contact = await _contactService.UpdateContactAsync(id, updateDto, currentUser);

                if (contact == null)
                {
                    return NotFound($"Contact with ID {id} not found");
                }

                return Ok(contact);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating contact with ID {ContactId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact with ID {ContactId}", id);
                return StatusCode(500, "An error occurred while updating the contact");
            }
        }

        /// <summary>
        /// Delete a contact (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteContact(int id)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _contactService.DeleteContactAsync(id, currentUser);

                if (!success)
                {
                    return NotFound($"Contact with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact with ID {ContactId}", id);
                return StatusCode(500, "An error occurred while deleting the contact");
            }
        }

        /// <summary>
        /// Search contacts
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ContactListResponseDto>> SearchContacts(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _contactService.SearchContactsAsync(searchTerm, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching contacts with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching contacts");
            }
        }

        /// <summary>
        /// Check if contact exists by email
        /// </summary>
        [HttpGet("exists/{email}")]
        public async Task<ActionResult<bool>> ContactExists(string email)
        {
            try
            {
                var exists = await _contactService.ContactExistsAsync(email);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if contact exists with email {Email}", email);
                return StatusCode(500, "An error occurred while checking contact existence");
            }
        }

        /// <summary>
        /// Bulk import contacts
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<BulkImportResultDto>> BulkImportContacts([FromBody] BulkImportContactRequestDto importRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = GetCurrentUser();
                var result = await _contactService.BulkImportContactsAsync(importRequest, currentUser);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import of contacts");
                return StatusCode(500, "An error occurred during bulk import");
            }
        }

        /// <summary>
        /// Assign tag to contact
        /// </summary>
        [HttpPost("{contactId}/tags/{tagId}")]
        public async Task<ActionResult> AssignTagToContact(int contactId, int tagId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var success = await _contactService.AssignTagToContactAsync(contactId, tagId, currentUser);

                if (!success)
                {
                    return BadRequest("Failed to assign tag to contact");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning tag {TagId} to contact {ContactId}", tagId, contactId);
                return StatusCode(500, "An error occurred while assigning the tag");
            }
        }

        /// <summary>
        /// Remove tag from contact
        /// </summary>
        [HttpDelete("{contactId}/tags/{tagId}")]
        public async Task<ActionResult> RemoveTagFromContact(int contactId, int tagId)
        {
            try
            {
                var success = await _contactService.RemoveTagFromContactAsync(contactId, tagId);

                if (!success)
                {
                    return NotFound("Tag assignment not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {TagId} from contact {ContactId}", tagId, contactId);
                return StatusCode(500, "An error occurred while removing the tag");
            }
        }

        private string GetCurrentUser()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? 
                   User.FindFirst(ClaimTypes.Name)?.Value ?? 
                   User.FindFirst("email")?.Value ?? 
                   "system";
        }
    }
}
