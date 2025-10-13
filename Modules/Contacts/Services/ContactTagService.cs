using MyApi.Data;
using MyApi.Modules.Contacts.DTOs;
using MyApi.Modules.Contacts.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Contacts.Services
{
    public class ContactTagService : IContactTagService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactTagService> _logger;

        public ContactTagService(ApplicationDbContext context, ILogger<ContactTagService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContactTagListResponseDto> GetAllTagsAsync()
        {
            try
            {
                var tags = await _context.ContactTags
                    .Include(t => t.ContactAssignments)
                    .Where(t => !t.IsDeleted)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                var tagDtos = tags.Select(MapToTagDto).ToList();

                return new ContactTagListResponseDto
                {
                    Tags = tagDtos,
                    TotalCount = tagDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contact tags");
                throw;
            }
        }

        public async Task<ContactTagDto?> GetTagByIdAsync(int id)
        {
            try
            {
                var tag = await _context.ContactTags
                    .Include(t => t.ContactAssignments)
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                return tag != null ? MapToTagDto(tag) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact tag by id {TagId}", id);
                throw;
            }
        }

        public async Task<ContactTagDto> CreateTagAsync(CreateContactTagRequestDto createDto)
        {
            try
            {
                // Check if tag name already exists
                var existingTag = await _context.ContactTags
                    .Where(t => t.Name.ToLower() == createDto.Name.ToLower() && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingTag != null)
                {
                    throw new InvalidOperationException("A tag with this name already exists");
                }

                var tag = new ContactTag
                {
                    Name = createDto.Name,
                    Color = createDto.Color,
                    Description = createDto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ContactTags.Add(tag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact tag created successfully with ID {TagId}", tag.Id);
                return MapToTagDto(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact tag");
                throw;
            }
        }

        public async Task<ContactTagDto?> UpdateTagAsync(int id, UpdateContactTagRequestDto updateDto)
        {
            try
            {
                var tag = await _context.ContactTags
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (tag == null)
                {
                    return null;
                }

                // Check name uniqueness if name is being changed
                if (!string.IsNullOrEmpty(updateDto.Name) && 
                    updateDto.Name.ToLower() != tag.Name.ToLower())
                {
                    var existingTag = await _context.ContactTags
                        .Where(t => t.Name.ToLower() == updateDto.Name.ToLower() && !t.IsDeleted && t.Id != id)
                        .FirstOrDefaultAsync();

                    if (existingTag != null)
                    {
                        throw new InvalidOperationException("A tag with this name already exists");
                    }

                    tag.Name = updateDto.Name;
                }

                if (!string.IsNullOrEmpty(updateDto.Color))
                    tag.Color = updateDto.Color;

                if (updateDto.Description != null)
                    tag.Description = updateDto.Description;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact tag updated successfully with ID {TagId}", id);
                
                // Reload with contact assignments for accurate count
                var updatedTag = await GetTagByIdAsync(id);
                return updatedTag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact tag with ID {TagId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            try
            {
                var tag = await _context.ContactTags
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (tag == null)
                {
                    return false;
                }

                // Remove all tag assignments first
                var assignments = await _context.Set<ContactTagAssignment>()
                    .Where(ta => ta.TagId == id)
                    .ToListAsync();

                _context.Set<ContactTagAssignment>().RemoveRange(assignments);

                // Soft delete the tag
                tag.IsDeleted = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact tag soft deleted successfully with ID {TagId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact tag with ID {TagId}", id);
                throw;
            }
        }

        public async Task<bool> TagExistsAsync(string name)
        {
            try
            {
                return await _context.ContactTags
                    .AnyAsync(t => t.Name.ToLower() == name.ToLower() && !t.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if contact tag exists with name {TagName}", name);
                throw;
            }
        }

        private static ContactTagDto MapToTagDto(ContactTag tag)
        {
            return new ContactTagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Color = tag.Color,
                Description = tag.Description,
                CreatedAt = tag.CreatedAt,
                ContactCount = tag.ContactAssignments?.Count ?? 0
            };
        }
    }
}
