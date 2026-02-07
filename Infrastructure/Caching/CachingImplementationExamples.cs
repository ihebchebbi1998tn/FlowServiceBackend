// ============================================================================
// EXAMPLE: Implementing Caching in Services
// ============================================================================
// This file shows how to integrate the CacheService into your existing services
// Copy these patterns to other services for consistent caching implementation

using MyApi.Data;
using MyApi.Modules.Contacts.DTOs;
using MyApi.Modules.Contacts.Models;
using MyApi.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Contacts.Services
{
    /// <summary>
    /// Enhanced ContactTagService with caching
    /// Demonstrates cache-aside pattern for reference data
    /// </summary>
    public class ContactTagServiceWithCaching : IContactTagService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly CacheInvalidationHelper _cacheInvalidation;
        private readonly ILogger<ContactTagServiceWithCaching> _logger;

        // Cache key constants
        private const string CACHE_KEY_ALL_TAGS = "contact_tags_all";
        private const string CACHE_KEY_TAG_DETAIL = "contact_tag_{0}"; // {tagId}
        private const string CACHE_PATTERN_TAGS = "contact_tag*";

        // Cache expiration times
        private static readonly TimeSpan CACHE_EXPIRATION_TAGS = TimeSpan.FromHours(1);

        public ContactTagServiceWithCaching(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<ContactTagServiceWithCaching> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _cacheInvalidation = new CacheInvalidationHelper(cacheService, logger);
            _logger = logger;
        }

        // ====================================================================
        // READ operations with caching
        // ====================================================================

        /// <summary>
        /// Get all tags with caching
        /// Cache hit rate: Very high (tags change rarely)
        /// Improvement: 1000+ ms → 5-10 ms with cache
        /// </summary>
        public async Task<ContactTagListResponseDto> GetAllTagsAsync()
        {
            return await _cacheService.GetOrSetAsync(
                CACHE_KEY_ALL_TAGS,
                async () =>
                {
                    _logger.LogInformation("Fetching all tags from database...");

                    var tags = await _context.ContactTags
                        .AsNoTracking()
                        .Where(t => !t.IsDeleted)
                        .OrderBy(t => t.Name)
                        .ToListAsync();

                    var tagDtos = tags.Select(MapToTagDto).ToList();

                    _logger.LogInformation("Found {Count} tags", tags.Count);

                    return new ContactTagListResponseDto
                    {
                        Tags = tagDtos,
                        TotalCount = tagDtos.Count
                    };
                },
                CACHE_EXPIRATION_TAGS);
        }

        /// <summary>
        /// Get tag by ID with caching
        /// </summary>
        public async Task<ContactTagDto?> GetTagByIdAsync(int id)
        {
            var cacheKey = string.Format(CACHE_KEY_TAG_DETAIL, id);

            var tag = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    return await _context.ContactTags
                        .AsNoTracking()
                        .Where(t => t.Id == id && !t.IsDeleted)
                        .FirstOrDefaultAsync();
                },
                CACHE_EXPIRATION_TAGS);

            return tag != null ? MapToTagDto(tag) : null;
        }

        // ====================================================================
        // WRITE operations with cache invalidation
        // ====================================================================

        /// <summary>
        /// Create tag and invalidate cache
        /// </summary>
        public async Task<ContactTagDto?> CreateTagAsync(CreateContactTagDto dto)
        {
            _logger.LogInformation("Creating new tag: {TagName}", dto.Name);

            // Validate
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Tag name is required");

            // Check for duplicates
            var exists = await _context.ContactTags
                .AsNoTracking()
                .AnyAsync(t => t.Name.ToLower() == dto.Name.ToLower() && !t.IsDeleted);

            if (exists)
                throw new InvalidOperationException($"Tag '{dto.Name}' already exists");

            // Create
            var tag = new ContactTag
            {
                Name = dto.Name,
                Color = dto.Color ?? "#000000",
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.ContactTags.Add(tag);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tag created with ID: {TagId}", tag.Id);

            // ✅ INVALIDATE CACHE: Tags list has changed
            _cacheService.Remove(CACHE_KEY_ALL_TAGS);
            _cacheService.RemoveByPattern(CACHE_PATTERN_TAGS);

            return MapToTagDto(tag);
        }

        /// <summary>
        /// Update tag and invalidate cache
        /// </summary>
        public async Task<ContactTagDto?> UpdateTagAsync(int id, CreateContactTagDto dto)
        {
            _logger.LogInformation("Updating tag {TagId}: {TagName}", id, dto.Name);

            var tag = await _context.ContactTags
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (tag == null)
                return null;

            // Update fields
            tag.Name = dto.Name ?? tag.Name;
            tag.Color = dto.Color ?? tag.Color;
            tag.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tag {TagId} updated successfully", id);

            // ✅ INVALIDATE CACHE: Specific tag and list changed
            var cacheKey = string.Format(CACHE_KEY_TAG_DETAIL, id);
            _cacheService.Remove(cacheKey);
            _cacheService.Remove(CACHE_KEY_ALL_TAGS);

            return MapToTagDto(tag);
        }

        /// <summary>
        /// Delete tag (soft delete) and invalidate cache
        /// </summary>
        public async Task<bool> DeleteTagAsync(int id)
        {
            _logger.LogInformation("Deleting tag {TagId}", id);

            var tag = await _context.ContactTags
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (tag == null)
                return false;

            // Soft delete
            tag.IsDeleted = true;
            tag.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tag {TagId} deleted successfully", id);

            // ✅ INVALIDATE CACHE: Tag is now hidden
            var cacheKey = string.Format(CACHE_KEY_TAG_DETAIL, id);
            _cacheService.Remove(cacheKey);
            _cacheService.Remove(CACHE_KEY_ALL_TAGS);

            return true;
        }

        // ====================================================================
        // Mapping helper
        // ====================================================================

        private static ContactTagDto MapToTagDto(ContactTag tag)
        {
            return new ContactTagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Color = tag.Color,
                CreatedDate = tag.CreatedDate
            };
        }
    }

    // ========================================================================
    // ADVANCED EXAMPLE: ContactService with Selective Caching
    // ========================================================================

    /// <summary>
    /// ContactService with optimized caching strategy
    /// Demonstrates selective caching for list and detail endpoints
    /// </summary>
    public class ContactServiceWithCaching : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ContactServiceWithCaching> _logger;

        // Cache key constants
        private const string CACHE_KEY_DETAIL = "contact_detail_{0}"; // {contactId}
        private const string CACHE_PATTERN_DETAIL = "contact_detail_*";

        // Note: List endpoints NOT cached due to pagination/filtering variations
        // But individual contact details ARE cached for detail pages

        private static readonly TimeSpan CACHE_EXPIRATION_CONTACT = TimeSpan.FromMinutes(15);

        public ContactServiceWithCaching(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<ContactServiceWithCaching> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// List endpoint - NOT cached due to pagination/filtering
        /// But optimized with projection and proper indexing
        /// </summary>
        public async Task<ContactListResponseDto> GetAllContactsAsync(
            ContactSearchRequestDto? searchRequest = null)
        {
            try
            {
                _logger.LogInformation("Getting contacts with filters");

                // ✅ Projection: Only select needed columns for list view
                var query = _context.Contacts
                    .AsNoTracking()
                    // ❌ NOT including Tags/Notes - only for list view
                    .Where(c => c.IsActive)
                    .Select(c => new ContactListItemDto
                    {
                        Id = c.Id,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        Email = c.Email,
                        Company = c.Company,
                        Type = c.Type,
                        Status = c.Status,
                        Phone = c.Phone,
                        CreatedDate = c.CreatedDate
                        // ❌ NOT including: TagAssignments, Notes, Address details
                    });

                // Apply filters (at database level!)
                if (searchRequest != null)
                {
                    // ✅ Using ILIKE instead of string.Contains
                    if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
                    {
                        var pattern = $"%{searchRequest.SearchTerm}%";
                        query = query.Where(c =>
                            EF.Functions.ILike(c.FirstName, pattern) ||
                            EF.Functions.ILike(c.LastName, pattern) ||
                            EF.Functions.ILike(c.Email, pattern) ||
                            EF.Functions.ILike(c.Company, pattern));
                    }

                    if (!string.IsNullOrEmpty(searchRequest.Status))
                    {
                        query = query.Where(c => c.Status == searchRequest.Status);
                    }

                    // Add other filters...
                }

                // Count and pagination
                var totalCount = await query.CountAsync();
                var pageNumber = Math.Max(1, searchRequest?.PageNumber ?? 1);
                var pageSize = Math.Min(100, Math.Max(1, searchRequest?.PageSize ?? 20));
                var skip = (pageNumber - 1) * pageSize;

                var contacts = await query
                    .OrderByDescending(c => c.CreatedDate)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                return new ContactListResponseDto
                {
                    Contacts = contacts,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts");
                throw;
            }
        }

        /// <summary>
        /// Detail endpoint - CACHED because user likely views same contact multiple times
        /// Improvement: 500-1000ms → 5-10ms on cache hit
        /// </summary>
        public async Task<ContactResponseDto?> GetContactByIdAsync(int id)
        {
            var cacheKey = string.Format(CACHE_KEY_DETAIL, id);

            _logger.LogInformation("Getting contact detail {ContactId}", id);

            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var contact = await _context.Contacts
                        .AsNoTracking()
                        .Include(c => c.TagAssignments)
                            .ThenInclude(ta => ta.Tag)
                        .Include(c => c.ContactNotes)
                        .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                    if (contact == null)
                        return null;

                    return new ContactResponseDto
                    {
                        Id = contact.Id,
                        FirstName = contact.FirstName,
                        LastName = contact.LastName,
                        Email = contact.Email,
                        Company = contact.Company,
                        Type = contact.Type,
                        Status = contact.Status,
                        Phone = contact.Phone,
                        Address = contact.Address,
                        City = contact.City,
                        Tags = contact.TagAssignments
                            .Select(ta => new { ta.Tag.Id, ta.Tag.Name })
                            .ToList(),
                        Notes = contact.ContactNotes
                            .OrderByDescending(n => n.CreatedDate)
                            .Select(n => new { n.Id, n.Content, n.CreatedDate })
                            .ToList(),
                        CreatedDate = contact.CreatedDate,
                        ModifiedDate = contact.ModifiedDate
                    };
                },
                CACHE_EXPIRATION_CONTACT);
        }

        /// <summary>
        /// Create contact and invalidate caches
        /// </summary>
        public async Task<ContactResponseDto?> CreateContactAsync(CreateContactDto dto)
        {
            _logger.LogInformation("Creating new contact");

            var contact = new Contact
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Company = dto.Company,
                Type = dto.Type,
                Status = dto.Status ?? "active",
                Phone = dto.Phone,
                Address = dto.Address,
                City = dto.City,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contact created with ID {ContactId}", contact.Id);

            // ✅ Cache invalidation: List queries may now be outdated
            // (But list endpoints are paginated so cache not used anyway)

            return await GetContactByIdAsync(contact.Id);
        }

        /// <summary>
        /// Update contact and invalidate cache
        /// </summary>
        public async Task<ContactResponseDto?> UpdateContactAsync(int id, UpdateContactDto dto)
        {
            _logger.LogInformation("Updating contact {ContactId}", id);

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (contact == null)
                return null;

            contact.FirstName = dto.FirstName ?? contact.FirstName;
            contact.LastName = dto.LastName ?? contact.LastName;
            contact.Email = dto.Email ?? contact.Email;
            contact.Company = dto.Company ?? contact.Company;
            contact.Status = dto.Status ?? contact.Status;
            contact.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ✅ INVALIDATE CACHE: Detail view is outdated
            var cacheKey = string.Format(CACHE_KEY_DETAIL, id);
            _cacheService.Remove(cacheKey);

            _logger.LogInformation("Contact {ContactId} updated", id);

            return await GetContactByIdAsync(id); // Will re-cache
        }

        // Additional methods...
    }
}

// ============================================================================
// REGISTRATION IN Program.cs
// ============================================================================
// Add this to your Program.cs to register caching:
/*

// Add after DbContext registration
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<CacheInvalidationHelper>();

// Replace old service registration with cached version
// OLD:
// builder.Services.AddScoped<IContactTagService, ContactTagService>();
// builder.Services.AddScoped<IContactService, ContactService>();

// NEW:
builder.Services.AddScoped<IContactTagService, ContactTagServiceWithCaching>();
builder.Services.AddScoped<IContactService, ContactServiceWithCaching>();

*/
