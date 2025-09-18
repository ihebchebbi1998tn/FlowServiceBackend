using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Services.Contacts
{
    public class ContactManagementService : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactManagementService> _logger;

        public ContactManagementService(ApplicationDbContext context, ILogger<ContactManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContactListResponseDto> GetAllContactsAsync(ContactSearchRequestDto? searchRequest = null)
        {
            try
            {
                var query = _context.Contacts
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.TagAssignments)
                        .ThenInclude(ta => ta.Tag)
                    .AsQueryable();

                // Apply search filters
                if (searchRequest != null)
                {
                    if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
                    {
                        query = query.Where(c => c.Name.Contains(searchRequest.SearchTerm) || 
                                               c.Email.Contains(searchRequest.SearchTerm));
                    }

                    if (!string.IsNullOrEmpty(searchRequest.Status))
                    {
                        query = query.Where(c => c.Status == searchRequest.Status);
                    }

                    if (!string.IsNullOrEmpty(searchRequest.Type))
                    {
                        query = query.Where(c => c.Type == searchRequest.Type);
                    }
                }

                var contacts = await query
                    .OrderBy(c => c.Name)
                    .Select(c => new ContactResponseDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Email = c.Email,
                        Phone = c.Phone,
                        Company = c.Company,
                        Status = c.Status,
                        Type = c.Type,
                        Tags = c.TagAssignments.Select(ta => new ContactTagDto
                        {
                            Id = ta.Tag.Id,
                            Name = ta.Tag.Name,
                            Color = ta.Tag.Color
                        }).ToList(),
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                return new ContactListResponseDto
                {
                    Contacts = contacts,
                    TotalCount = contacts.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contacts");
                throw;
            }
        }

        // ... implement other methods from IContactService
        // Move logic from original ContactService.cs

        public async Task<ContactResponseDto?> GetContactByIdAsync(int id)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<ContactResponseDto> CreateContactAsync(CreateContactRequestDto createDto, string createdByUser)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<ContactResponseDto?> UpdateContactAsync(int id, UpdateContactRequestDto updateDto, string modifiedByUser)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<bool> DeleteContactAsync(int id, string deletedByUser)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<bool> ContactExistsAsync(string email)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<BulkImportResultDto> BulkImportContactsAsync(BulkImportContactRequestDto importRequest, string createdByUser)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<bool> AssignTagToContactAsync(int contactId, int tagId, string assignedByUser)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<bool> RemoveTagFromContactAsync(int contactId, int tagId)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }

        public async Task<ContactListResponseDto> SearchContactsAsync(string searchTerm, int pageNumber = 1, int pageSize = 20)
        {
            throw new NotImplementedException("Move from original ContactService.cs");
        }
    }
}