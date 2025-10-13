using MyApi.Modules.Lookups.DTOs;
using MyApi.Modules.Lookups.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Lookups.Services
{
    public class LookupService : ILookupService
    {
        private readonly ApplicationDbContext _context;

        public LookupService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Article Categories
        public async Task<LookupListResponseDto> GetArticleCategoriesAsync()
        {
            var items = await _context.LookupItems
                .Where(x => x.LookupType == "article-category" && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return new LookupListResponseDto
            {
                items = items.Select(MapToDto).ToList(),
                totalCount = items.Count
            };
        }

        public async Task<LookupItemDto?> GetArticleCategoryByIdAsync(string id)
        {
            var item = await _context.LookupItems
                .FirstOrDefaultAsync(x => x.Id == id && x.LookupType == "article-category" && !x.IsDeleted);

            return item != null ? MapToDto(item) : null;
        }

        public async Task<LookupItemDto> CreateArticleCategoryAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            var entity = new LookupItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = createDto.Name,
                Description = createDto.Description,
                Color = createDto.Color,
                LookupType = "article-category",
                IsActive = createDto.IsActive,
                SortOrder = createDto.SortOrder,
                CreatedUser = createdByUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.LookupItems.Add(entity);
            await _context.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<LookupItemDto?> UpdateArticleCategoryAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            var entity = await _context.LookupItems
                .FirstOrDefaultAsync(x => x.Id == id && x.LookupType == "article-category" && !x.IsDeleted);

            if (entity == null) return null;

            if (updateDto.Name != null) entity.Name = updateDto.Name;
            if (updateDto.Description != null) entity.Description = updateDto.Description;
            if (updateDto.Color != null) entity.Color = updateDto.Color;
            if (updateDto.IsActive.HasValue) entity.IsActive = updateDto.IsActive.Value;
            if (updateDto.SortOrder.HasValue) entity.SortOrder = updateDto.SortOrder.Value;

            entity.ModifyUser = modifiedByUser;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteArticleCategoryAsync(string id, string deletedByUser)
        {
            var entity = await _context.LookupItems
                .FirstOrDefaultAsync(x => x.Id == id && x.LookupType == "article-category" && !x.IsDeleted);

            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.ModifyUser = deletedByUser;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Article Statuses
        public async Task<LookupListResponseDto> GetArticleStatusesAsync()
        {
            return await GetLookupsByTypeAsync("article-status");
        }

        public async Task<LookupItemDto?> GetArticleStatusByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "article-status");
        }

        public async Task<LookupItemDto> CreateArticleStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "article-status", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateArticleStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "article-status", modifiedByUser);
        }

        public async Task<bool> DeleteArticleStatusAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "article-status", deletedByUser);
        }

        // Service Categories
        public async Task<LookupListResponseDto> GetServiceCategoriesAsync()
        {
            return await GetLookupsByTypeAsync("service-category");
        }

        public async Task<LookupItemDto?> GetServiceCategoryByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "service-category");
        }

        public async Task<LookupItemDto> CreateServiceCategoryAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "service-category", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateServiceCategoryAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "service-category", modifiedByUser);
        }

        public async Task<bool> DeleteServiceCategoryAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "service-category", deletedByUser);
        }

        // Task Statuses
        public async Task<LookupListResponseDto> GetTaskStatusesAsync()
        {
            return await GetLookupsByTypeAsync("task-status");
        }

        public async Task<LookupItemDto?> GetTaskStatusByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "task-status");
        }

        public async Task<LookupItemDto> CreateTaskStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "task-status", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateTaskStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "task-status", modifiedByUser);
        }

        public async Task<bool> DeleteTaskStatusAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "task-status", deletedByUser);
        }

        // Event Types
        public async Task<LookupListResponseDto> GetEventTypesAsync()
        {
            return await GetLookupsByTypeAsync("event-type");
        }

        public async Task<LookupItemDto?> GetEventTypeByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "event-type");
        }

        public async Task<LookupItemDto> CreateEventTypeAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "event-type", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateEventTypeAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "event-type", modifiedByUser);
        }

        public async Task<bool> DeleteEventTypeAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "event-type", deletedByUser);
        }

        // Priorities
        public async Task<LookupListResponseDto> GetPrioritiesAsync()
        {
            return await GetLookupsByTypeAsync("priority");
        }

        public async Task<LookupItemDto?> GetPriorityByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "priority");
        }

        public async Task<LookupItemDto> CreatePriorityAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "priority", createdByUser);
        }

        public async Task<LookupItemDto?> UpdatePriorityAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "priority", modifiedByUser);
        }

        public async Task<bool> DeletePriorityAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "priority", deletedByUser);
        }

        // Technician Statuses
        public async Task<LookupListResponseDto> GetTechnicianStatusesAsync()
        {
            return await GetLookupsByTypeAsync("technician-status");
        }

        public async Task<LookupItemDto?> GetTechnicianStatusByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "technician-status");
        }

        public async Task<LookupItemDto> CreateTechnicianStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "technician-status", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateTechnicianStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "technician-status", modifiedByUser);
        }

        public async Task<bool> DeleteTechnicianStatusAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "technician-status", deletedByUser);
        }

        // Leave Types
        public async Task<LookupListResponseDto> GetLeaveTypesAsync()
        {
            return await GetLookupsByTypeAsync("leave-type");
        }

        public async Task<LookupItemDto?> GetLeaveTypeByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "leave-type");
        }

        public async Task<LookupItemDto> CreateLeaveTypeAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "leave-type", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateLeaveTypeAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "leave-type", modifiedByUser);
        }

        public async Task<bool> DeleteLeaveTypeAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "leave-type", deletedByUser);
        }

        // Project Statuses
        public async Task<LookupListResponseDto> GetProjectStatusesAsync()
        {
            return await GetLookupsByTypeAsync("project-status");
        }

        public async Task<LookupItemDto?> GetProjectStatusByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "project-status");
        }

        public async Task<LookupItemDto> CreateProjectStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "project-status", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateProjectStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "project-status", modifiedByUser);
        }

        public async Task<bool> DeleteProjectStatusAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "project-status", deletedByUser);
        }

        // Project Types
        public async Task<LookupListResponseDto> GetProjectTypesAsync()
        {
            return await GetLookupsByTypeAsync("project-type");
        }

        public async Task<LookupItemDto?> GetProjectTypeByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "project-type");
        }

        public async Task<LookupItemDto> CreateProjectTypeAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "project-type", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateProjectTypeAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "project-type", modifiedByUser);
        }

        public async Task<bool> DeleteProjectTypeAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "project-type", deletedByUser);
        }

        // Offer Statuses
        public async Task<LookupListResponseDto> GetOfferStatusesAsync()
        {
            return await GetLookupsByTypeAsync("offer-status");
        }

        public async Task<LookupItemDto?> GetOfferStatusByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "offer-status");
        }

        public async Task<LookupItemDto> CreateOfferStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "offer-status", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateOfferStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "offer-status", modifiedByUser);
        }

        public async Task<bool> DeleteOfferStatusAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "offer-status", deletedByUser);
        }

        // Skills
        public async Task<LookupListResponseDto> GetSkillsAsync()
        {
            return await GetLookupsByTypeAsync("skill");
        }

        public async Task<LookupItemDto?> GetSkillByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "skill");
        }

        public async Task<LookupItemDto> CreateSkillAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "skill", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateSkillAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "skill", modifiedByUser);
        }

        public async Task<bool> DeleteSkillAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "skill", deletedByUser);
        }

        // Countries
        public async Task<LookupListResponseDto> GetCountriesAsync()
        {
            return await GetLookupsByTypeAsync("country");
        }

        public async Task<LookupItemDto?> GetCountryByIdAsync(string id)
        {
            return await GetLookupByIdAsync(id, "country");
        }

        public async Task<LookupItemDto> CreateCountryAsync(CreateLookupItemRequestDto createDto, string createdByUser)
        {
            return await CreateLookupAsync(createDto, "country", createdByUser);
        }

        public async Task<LookupItemDto?> UpdateCountryAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser)
        {
            return await UpdateLookupAsync(id, updateDto, "country", modifiedByUser);
        }

        public async Task<bool> DeleteCountryAsync(string id, string deletedByUser)
        {
            return await DeleteLookupAsync(id, "country", deletedByUser);
        }

        // Currencies
        public async Task<CurrencyListResponseDto> GetCurrenciesAsync()
        {
            var currencies = await _context.Currencies
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return new CurrencyListResponseDto
            {
                currencies = currencies.Select(MapCurrencyToDto).ToList(),
                totalCount = currencies.Count
            };
        }

        public async Task<CurrencyDto?> GetCurrencyByIdAsync(string id)
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            return currency != null ? MapCurrencyToDto(currency) : null;
        }

        public async Task<CurrencyDto> CreateCurrencyAsync(CreateCurrencyRequestDto createDto, string createdByUser)
        {
            var entity = new Currency
            {
                Id = createDto.Code.ToUpper(),
                Name = createDto.Name,
                Symbol = createDto.Symbol,
                Code = createDto.Code.ToUpper(),
                IsActive = createDto.IsActive,
                IsDefault = createDto.IsDefault,
                SortOrder = createDto.SortOrder,
                CreatedUser = createdByUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.Currencies.Add(entity);
            await _context.SaveChangesAsync();

            return MapCurrencyToDto(entity);
        }

        public async Task<CurrencyDto?> UpdateCurrencyAsync(string id, UpdateCurrencyRequestDto updateDto, string modifiedByUser)
        {
            var entity = await _context.Currencies
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return null;

            if (updateDto.Name != null) entity.Name = updateDto.Name;
            if (updateDto.Symbol != null) entity.Symbol = updateDto.Symbol;
            if (updateDto.Code != null) entity.Code = updateDto.Code.ToUpper();
            if (updateDto.IsActive.HasValue) entity.IsActive = updateDto.IsActive.Value;
            if (updateDto.IsDefault.HasValue) entity.IsDefault = updateDto.IsDefault.Value;
            if (updateDto.SortOrder.HasValue) entity.SortOrder = updateDto.SortOrder.Value;

            entity.ModifyUser = modifiedByUser;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapCurrencyToDto(entity);
        }

        public async Task<bool> DeleteCurrencyAsync(string id, string deletedByUser)
        {
            var entity = await _context.Currencies
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.ModifyUser = deletedByUser;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Helper methods
        private async Task<LookupListResponseDto> GetLookupsByTypeAsync(string lookupType)
        {
            var items = await _context.LookupItems
                .Where(x => x.LookupType == lookupType && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return new LookupListResponseDto
            {
                items = items.Select(MapToDto).ToList(),
                totalCount = items.Count
            };
        }

        private async Task<LookupItemDto?> GetLookupByIdAsync(string id, string lookupType)
        {
            var item = await _context.LookupItems
                .FirstOrDefaultAsync(x => x.Id == id && x.LookupType == lookupType && !x.IsDeleted);

            return item != null ? MapToDto(item) : null;
        }

        private async Task<LookupItemDto> CreateLookupAsync(CreateLookupItemRequestDto createDto, string lookupType, string createdByUser)
        {
            var entity = new LookupItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = createDto.Name,
                Description = createDto.Description,
                Color = createDto.Color,
                LookupType = lookupType,
                IsActive = createDto.IsActive,
                SortOrder = createDto.SortOrder,
                CreatedUser = createdByUser,
                CreatedAt = DateTime.UtcNow,
                Level = createDto.Level,
                IsCompleted = createDto.IsCompleted,
                DefaultDuration = createDto.DefaultDuration,
                IsAvailable = createDto.IsAvailable,
                IsPaid = createDto.IsPaid,
                Category = createDto.Category
            };

            _context.LookupItems.Add(entity);
            await _context.SaveChangesAsync();

            return MapToDto(entity);
        }

        private async Task<LookupItemDto?> UpdateLookupAsync(string id, UpdateLookupItemRequestDto updateDto, string lookupType, string modifiedByUser)
        {
            var entity = await _context.LookupItems
                .FirstOrDefaultAsync(x => x.Id == id && x.LookupType == lookupType && !x.IsDeleted);

            if (entity == null) return null;

            if (updateDto.Name != null) entity.Name = updateDto.Name;
            if (updateDto.Description != null) entity.Description = updateDto.Description;
            if (updateDto.Color != null) entity.Color = updateDto.Color;
            if (updateDto.IsActive.HasValue) entity.IsActive = updateDto.IsActive.Value;
            if (updateDto.SortOrder.HasValue) entity.SortOrder = updateDto.SortOrder.Value;
            if (updateDto.Level.HasValue) entity.Level = updateDto.Level.Value;
            if (updateDto.IsCompleted.HasValue) entity.IsCompleted = updateDto.IsCompleted.Value;
            if (updateDto.DefaultDuration.HasValue) entity.DefaultDuration = updateDto.DefaultDuration.Value;
            if (updateDto.IsAvailable.HasValue) entity.IsAvailable = updateDto.IsAvailable.Value;
            if (updateDto.IsPaid.HasValue) entity.IsPaid = updateDto.IsPaid.Value;
            if (updateDto.Category != null) entity.Category = updateDto.Category;

            entity.ModifyUser = modifiedByUser;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        private async Task<bool> DeleteLookupAsync(string id, string lookupType, string deletedByUser)
        {
            var entity = await _context.LookupItems
                .FirstOrDefaultAsync(x => x.Id == id && x.LookupType == lookupType && !x.IsDeleted);

            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.ModifyUser = deletedByUser;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private static LookupItemDto MapToDto(LookupItem entity)
        {
            return new LookupItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Color = entity.Color,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedUser = entity.CreatedUser,
                ModifyUser = entity.ModifyUser,
                Level = entity.Level,
                IsCompleted = entity.IsCompleted,
                DefaultDuration = entity.DefaultDuration,
                IsAvailable = entity.IsAvailable,
                IsPaid = entity.IsPaid,
                Category = entity.Category
            };
        }

        private static CurrencyDto MapCurrencyToDto(Currency entity)
        {
            return new CurrencyDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Symbol = entity.Symbol,
                Code = entity.Code,
                IsActive = entity.IsActive,
                IsDefault = entity.IsDefault,
                SortOrder = entity.SortOrder,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedUser = entity.CreatedUser,
                ModifyUser = entity.ModifyUser
            };
        }
    }
}
