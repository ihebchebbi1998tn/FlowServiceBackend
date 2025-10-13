using MyApi.Data;
using MyApi.Modules.Contacts.DTOs;
using MyApi.Modules.Contacts.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Contacts.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactService> _logger;

        public ContactService(ApplicationDbContext context, ILogger<ContactService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContactListResponseDto> GetAllContactsAsync(ContactSearchRequestDto? searchRequest = null)
        {
            try
            {
                var query = _context.Contacts
                    .Include(c => c.TagAssignments)
                        .ThenInclude(ta => ta.Tag)
                    .Include(c => c.Notes)
                    .Where(c => !c.IsDeleted);

                // Apply filters
                if (searchRequest != null)
                {
                    if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
                    {
                        var searchTerm = searchRequest.SearchTerm.ToLower();
                        query = query.Where(c => c.Name.ToLower().Contains(searchTerm) ||
                                               c.Email.ToLower().Contains(searchTerm) ||
                                               (c.Company != null && c.Company.ToLower().Contains(searchTerm)));
                    }

                    if (!string.IsNullOrEmpty(searchRequest.Status))
                    {
                        query = query.Where(c => c.Status == searchRequest.Status);
                    }

                    if (!string.IsNullOrEmpty(searchRequest.Type))
                    {
                        query = query.Where(c => c.Type == searchRequest.Type);
                    }

                    if (searchRequest.Favorite.HasValue)
                    {
                        query = query.Where(c => c.Favorite == searchRequest.Favorite.Value);
                    }

                    if (searchRequest.TagIds != null && searchRequest.TagIds.Any())
                    {
                        query = query.Where(c => c.TagAssignments.Any(ta => searchRequest.TagIds.Contains(ta.TagId)));
                    }

                    // Apply sorting
                    if (!string.IsNullOrEmpty(searchRequest.SortBy))
                    {
                        var isDescending = searchRequest.SortDirection?.ToLower() == "desc";
                        
                        query = searchRequest.SortBy.ToLower() switch
                        {
                            "name" => isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                            "email" => isDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
                            "company" => isDescending ? query.OrderByDescending(c => c.Company) : query.OrderBy(c => c.Company),
                            "createdat" => isDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                            "lastcontactdate" => isDescending ? query.OrderByDescending(c => c.LastContactDate) : query.OrderBy(c => c.LastContactDate),
                            _ => query.OrderByDescending(c => c.CreatedAt)
                        };
                    }
                    else
                    {
                        query = query.OrderByDescending(c => c.CreatedAt);
                    }
                }
                else
                {
                    query = query.OrderByDescending(c => c.CreatedAt);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var pageNumber = searchRequest?.PageNumber ?? 1;
                var pageSize = searchRequest?.PageSize ?? 20;
                var skip = (pageNumber - 1) * pageSize;

                var contacts = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var contactDtos = contacts.Select(MapToContactDto).ToList();

                return new ContactListResponseDto
                {
                    Contacts = contactDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contacts");
                throw;
            }
        }

        public async Task<ContactResponseDto?> GetContactByIdAsync(int id)
        {
            try
            {
                var contact = await _context.Contacts
                    .Include(c => c.TagAssignments)
                        .ThenInclude(ta => ta.Tag)
                    .Include(c => c.Notes)
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                return contact != null ? MapToContactDto(contact) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact by id {ContactId}", id);
                throw;
            }
        }

        public async Task<ContactResponseDto> CreateContactAsync(CreateContactRequestDto createDto, string createdByUser)
        {
            try
            {
                // Check if email already exists
                var existingContact = await _context.Contacts
                    .Where(c => c.Email.ToLower() == createDto.Email.ToLower() && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingContact != null)
                {
                    throw new InvalidOperationException("A contact with this email already exists");
                }

                var contact = new Contact
                {
                    Name = createDto.Name,
                    Email = createDto.Email.ToLower(),
                    Phone = createDto.Phone,
                    Company = createDto.Company,
                    Position = createDto.Position,
                    Status = createDto.Status,
                    Type = createDto.Type,
                    Address = createDto.Address,
                    Avatar = createDto.Avatar,
                    Favorite = createDto.Favorite,
                    LastContactDate = createDto.LastContactDate,
                    CreatedBy = createdByUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                // Assign tags if provided
                if (createDto.TagIds.Any())
                {
                    foreach (var tagId in createDto.TagIds)
                    {
                        var tagAssignment = new ContactTagAssignment
                        {
                            ContactId = contact.Id,
                            TagId = tagId,
                            AssignedBy = createdByUser,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.Set<ContactTagAssignment>().Add(tagAssignment);
                    }
                    await _context.SaveChangesAsync();
                }

                // Reload contact with related data
                var createdContact = await GetContactByIdAsync(contact.Id);
                _logger.LogInformation("Contact created successfully with ID {ContactId}", contact.Id);
                
                return createdContact!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                throw;
            }
        }

        public async Task<ContactResponseDto?> UpdateContactAsync(int id, UpdateContactRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var contact = await _context.Contacts
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (contact == null)
                {
                    return null;
                }

                // Check email uniqueness if email is being changed
                if (!string.IsNullOrEmpty(updateDto.Email) && 
                    updateDto.Email.ToLower() != contact.Email.ToLower())
                {
                    var existingContact = await _context.Contacts
                        .Where(c => c.Email.ToLower() == updateDto.Email.ToLower() && !c.IsDeleted && c.Id != id)
                        .FirstOrDefaultAsync();

                    if (existingContact != null)
                    {
                        throw new InvalidOperationException("A contact with this email already exists");
                    }

                    contact.Email = updateDto.Email.ToLower();
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Name))
                    contact.Name = updateDto.Name;

                if (updateDto.Phone != null)
                    contact.Phone = updateDto.Phone;

                if (updateDto.Company != null)
                    contact.Company = updateDto.Company;

                if (updateDto.Position != null)
                    contact.Position = updateDto.Position;

                if (!string.IsNullOrEmpty(updateDto.Status))
                    contact.Status = updateDto.Status;

                if (!string.IsNullOrEmpty(updateDto.Type))
                    contact.Type = updateDto.Type;

                if (updateDto.Address != null)
                    contact.Address = updateDto.Address;

                if (updateDto.Avatar != null)
                    contact.Avatar = updateDto.Avatar;

                if (updateDto.Favorite.HasValue)
                    contact.Favorite = updateDto.Favorite.Value;

                if (updateDto.LastContactDate.HasValue)
                    contact.LastContactDate = updateDto.LastContactDate.Value;

                contact.ModifiedBy = modifiedByUser;
                contact.UpdatedAt = DateTime.UtcNow;

                // Update tags if provided
                if (updateDto.TagIds != null)
                {
                    // Remove existing tag assignments
                    var existingAssignments = await _context.Set<ContactTagAssignment>()
                        .Where(ta => ta.ContactId == id)
                        .ToListAsync();
                    
                    _context.Set<ContactTagAssignment>().RemoveRange(existingAssignments);

                    // Add new tag assignments
                    foreach (var tagId in updateDto.TagIds)
                    {
                        var tagAssignment = new ContactTagAssignment
                        {
                            ContactId = id,
                            TagId = tagId,
                            AssignedBy = modifiedByUser,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.Set<ContactTagAssignment>().Add(tagAssignment);
                    }
                }

                await _context.SaveChangesAsync();

                // Reload contact with related data
                var updatedContact = await GetContactByIdAsync(id);
                _logger.LogInformation("Contact updated successfully with ID {ContactId}", id);
                
                return updatedContact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact with ID {ContactId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteContactAsync(int id, string deletedByUser)
        {
            try
            {
                var contact = await _context.Contacts
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (contact == null)
                {
                    return false;
                }

                // Soft delete
                contact.IsDeleted = true;
                contact.ModifiedBy = deletedByUser;
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact soft deleted successfully with ID {ContactId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact with ID {ContactId}", id);
                throw;
            }
        }

        public async Task<bool> ContactExistsAsync(string email)
        {
            try
            {
                return await _context.Contacts
                    .AnyAsync(c => c.Email.ToLower() == email.ToLower() && !c.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if contact exists with email {Email}", email);
                throw;
            }
        }

        public async Task<BulkImportResultDto> BulkImportContactsAsync(BulkImportContactRequestDto importRequest, string createdByUser)
        {
            var result = new BulkImportResultDto
            {
                TotalProcessed = importRequest.Contacts.Count
            };

            try
            {
                foreach (var contactDto in importRequest.Contacts)
                {
                    try
                    {
                        // Check if contact exists
                        var existingContact = await _context.Contacts
                            .Where(c => c.Email.ToLower() == contactDto.Email.ToLower() && !c.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (existingContact != null)
                        {
                            if (importRequest.SkipDuplicates)
                            {
                                result.SkippedCount++;
                                continue;
                            }
                            else if (importRequest.UpdateExisting)
                            {
                                // Update existing contact
                                var updateDto = new UpdateContactRequestDto
                                {
                                    Name = contactDto.Name,
                                    Phone = contactDto.Phone,
                                    Company = contactDto.Company,
                                    Position = contactDto.Position,
                                    Status = contactDto.Status,
                                    Type = contactDto.Type,
                                    Address = contactDto.Address,
                                    Avatar = contactDto.Avatar,
                                    Favorite = contactDto.Favorite,
                                    LastContactDate = contactDto.LastContactDate,
                                    TagIds = contactDto.TagIds
                                };

                                var updatedContact = await UpdateContactAsync(existingContact.Id, updateDto, createdByUser);
                                if (updatedContact != null)
                                {
                                    result.ImportedContacts.Add(updatedContact);
                                    result.SuccessCount++;
                                }
                                else
                                {
                                    result.FailedCount++;
                                    result.Errors.Add($"Failed to update contact: {contactDto.Email}");
                                }
                            }
                            else
                            {
                                result.FailedCount++;
                                result.Errors.Add($"Duplicate email: {contactDto.Email}");
                            }
                        }
                        else
                        {
                            // Create new contact
                            var createdContact = await CreateContactAsync(contactDto, createdByUser);
                            result.ImportedContacts.Add(createdContact);
                            result.SuccessCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to process contact {contactDto.Email}: {ex.Message}");
                        _logger.LogWarning(ex, "Failed to import contact {Email}", contactDto.Email);
                    }
                }

                _logger.LogInformation("Bulk import completed. Success: {SuccessCount}, Failed: {FailedCount}, Skipped: {SkippedCount}", 
                    result.SuccessCount, result.FailedCount, result.SkippedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import");
                throw;
            }
        }

        public async Task<bool> AssignTagToContactAsync(int contactId, int tagId, string assignedByUser)
        {
            try
            {
                // Check if assignment already exists
                var existingAssignment = await _context.Set<ContactTagAssignment>()
                    .Where(ta => ta.ContactId == contactId && ta.TagId == tagId)
                    .FirstOrDefaultAsync();

                if (existingAssignment != null)
                {
                    return true; // Already assigned
                }

                var tagAssignment = new ContactTagAssignment
                {
                    ContactId = contactId,
                    TagId = tagId,
                    AssignedBy = assignedByUser,
                    AssignedAt = DateTime.UtcNow
                };

                _context.Set<ContactTagAssignment>().Add(tagAssignment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning tag {TagId} to contact {ContactId}", tagId, contactId);
                throw;
            }
        }

        public async Task<bool> RemoveTagFromContactAsync(int contactId, int tagId)
        {
            try
            {
                var assignment = await _context.Set<ContactTagAssignment>()
                    .Where(ta => ta.ContactId == contactId && ta.TagId == tagId)
                    .FirstOrDefaultAsync();

                if (assignment == null)
                {
                    return false;
                }

                _context.Set<ContactTagAssignment>().Remove(assignment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {TagId} from contact {ContactId}", tagId, contactId);
                throw;
            }
        }

        public async Task<ContactListResponseDto> SearchContactsAsync(string searchTerm, int pageNumber = 1, int pageSize = 20)
        {
            var searchRequest = new ContactSearchRequestDto
            {
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAllContactsAsync(searchRequest);
        }

        private static ContactResponseDto MapToContactDto(Contact contact)
        {
            return new ContactResponseDto
            {
                Id = contact.Id,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                Company = contact.Company,
                Position = contact.Position,
                Status = contact.Status,
                Type = contact.Type,
                Address = contact.Address,
                Avatar = contact.Avatar,
                Favorite = contact.Favorite,
                LastContactDate = contact.LastContactDate,
                CreatedAt = contact.CreatedAt,
                UpdatedAt = contact.UpdatedAt,
                CreatedBy = contact.CreatedBy,
                ModifiedBy = contact.ModifiedBy,
                Tags = contact.TagAssignments.Select(ta => new ContactTagDto
                {
                    Id = ta.Tag.Id,
                    Name = ta.Tag.Name,
                    Color = ta.Tag.Color,
                    Description = ta.Tag.Description,
                    CreatedAt = ta.Tag.CreatedAt
                }).ToList(),
                Notes = contact.Notes.OrderByDescending(n => n.CreatedAt).Select(n => new ContactNoteDto
                {
                    Id = n.Id,
                    ContactId = n.ContactId,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    CreatedBy = n.CreatedBy
                }).ToList()
            };
        }
    }
}
