using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;

namespace MyApi.Services
{
    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ArticleService> _logger;

        public ArticleService(ApplicationDbContext context, ILogger<ArticleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ArticleListResponseDto> GetAllArticlesAsync(ArticleSearchRequestDto? searchRequest = null)
        {
            var query = _context.Articles.AsQueryable();

            if (searchRequest != null)
            {
                if (!string.IsNullOrWhiteSpace(searchRequest.SearchTerm))
                {
                    var term = searchRequest.SearchTerm.ToLower();
                    query = query.Where(a => a.Name.ToLower().Contains(term)
                                          || (a.SKU != null && a.SKU.ToLower().Contains(term))
                                          || a.Category.ToLower().Contains(term));
                }
                if (!string.IsNullOrWhiteSpace(searchRequest.Type))
                {
                    query = query.Where(a => a.Type == searchRequest.Type);
                }
                if (!string.IsNullOrWhiteSpace(searchRequest.Category))
                {
                    query = query.Where(a => a.Category == searchRequest.Category);
                }
                if (!string.IsNullOrWhiteSpace(searchRequest.Status))
                {
                    query = query.Where(a => a.Status == searchRequest.Status);
                }
                if (searchRequest.LowStockOnly == true)
                {
                    query = query.Where(a => a.Type == "material" && a.Stock.HasValue && a.MinStock.HasValue && a.Stock < a.MinStock);
                }

                // Sorting
                var sortBy = searchRequest.SortBy?.ToLower();
                var sortDir = (searchRequest.SortDirection ?? "desc").ToLower();
                query = (sortBy, sortDir) switch
                {
                    ("name", "asc") => query.OrderBy(a => a.Name),
                    ("name", _) => query.OrderByDescending(a => a.Name),
                    ("createdat", "asc") => query.OrderBy(a => a.CreatedAt),
                    ("createdat", _) => query.OrderByDescending(a => a.CreatedAt),
                    ("updatedat", "asc") => query.OrderBy(a => a.UpdatedAt),
                    ("updatedat", _) => query.OrderByDescending(a => a.UpdatedAt),
                    _ => query.OrderByDescending(a => a.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(a => a.CreatedAt);
            }

            var totalCount = await query.CountAsync();
            var pageSize = searchRequest?.PageSize ?? 20;
            var pageNumber = searchRequest?.PageNumber ?? 1;
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new ArticleListResponseDto
            {
                Articles = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasNextPage = pageNumber * pageSize < totalCount,
                HasPreviousPage = pageNumber > 1
            };
        }

        public async Task<ArticleResponseDto?> GetArticleByIdAsync(Guid id)
        {
            var entity = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<ArticleResponseDto> CreateArticleAsync(CreateArticleRequestDto createDto)
        {
            // Basic type validation
            if (createDto.Type != "material" && createDto.Type != "service")
            {
                throw new InvalidOperationException("Type must be 'material' or 'service'.");
            }

            var entity = new Article
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                Category = createDto.Category,
                Type = createDto.Type,
                Status = createDto.Status,
                Tags = createDto.Tags,
                Notes = createDto.Notes,

                SKU = createDto.SKU,
                Stock = createDto.Stock,
                MinStock = createDto.MinStock,
                CostPrice = createDto.CostPrice,
                SellPrice = createDto.SellPrice,
                Supplier = createDto.Supplier,
                Location = createDto.Location,
                SubLocation = createDto.SubLocation,

                BasePrice = createDto.BasePrice,
                Duration = createDto.Duration,
                SkillsRequired = createDto.SkillsRequired,
                MaterialsNeeded = createDto.MaterialsNeeded,
                PreferredUsers = createDto.PreferredUsers,
                HourlyRate = createDto.HourlyRate,
                EstimatedDuration = createDto.EstimatedDuration,
                MaterialsIncluded = createDto.MaterialsIncluded,
                WarrantyCoverage = createDto.WarrantyCoverage,
                ServiceArea = createDto.ServiceArea,
                Inclusions = createDto.Inclusions,
                AddOnServices = createDto.AddOnServices,

                CreatedBy = createDto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Articles.Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ArticleResponseDto?> UpdateArticleAsync(Guid id, UpdateArticleRequestDto updateDto)
        {
            var entity = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return null;

            // Update fields if provided
            if (updateDto.Name != null) entity.Name = updateDto.Name;
            if (updateDto.Description != null) entity.Description = updateDto.Description;
            if (updateDto.Category != null) entity.Category = updateDto.Category;
            if (updateDto.Type != null) entity.Type = updateDto.Type;
            if (updateDto.Status != null) entity.Status = updateDto.Status;
            if (updateDto.Tags != null) entity.Tags = updateDto.Tags;
            if (updateDto.Notes != null) entity.Notes = updateDto.Notes;

            if (updateDto.SKU != null) entity.SKU = updateDto.SKU;
            if (updateDto.Stock.HasValue) entity.Stock = updateDto.Stock;
            if (updateDto.MinStock.HasValue) entity.MinStock = updateDto.MinStock;
            if (updateDto.CostPrice.HasValue) entity.CostPrice = updateDto.CostPrice;
            if (updateDto.SellPrice.HasValue) entity.SellPrice = updateDto.SellPrice;
            if (updateDto.Supplier != null) entity.Supplier = updateDto.Supplier;
            if (updateDto.Location != null) entity.Location = updateDto.Location;
            if (updateDto.SubLocation != null) entity.SubLocation = updateDto.SubLocation;

            if (updateDto.BasePrice.HasValue) entity.BasePrice = updateDto.BasePrice;
            if (updateDto.Duration.HasValue) entity.Duration = updateDto.Duration;
            if (updateDto.SkillsRequired != null) entity.SkillsRequired = updateDto.SkillsRequired;
            if (updateDto.MaterialsNeeded != null) entity.MaterialsNeeded = updateDto.MaterialsNeeded;
            if (updateDto.PreferredUsers != null) entity.PreferredUsers = updateDto.PreferredUsers;
            if (updateDto.HourlyRate.HasValue) entity.HourlyRate = updateDto.HourlyRate;
            if (updateDto.EstimatedDuration != null) entity.EstimatedDuration = updateDto.EstimatedDuration;
            if (updateDto.MaterialsIncluded.HasValue) entity.MaterialsIncluded = updateDto.MaterialsIncluded;
            if (updateDto.WarrantyCoverage != null) entity.WarrantyCoverage = updateDto.WarrantyCoverage;
            if (updateDto.ServiceArea != null) entity.ServiceArea = updateDto.ServiceArea;
            if (updateDto.Inclusions != null) entity.Inclusions = updateDto.Inclusions;
            if (updateDto.AddOnServices != null) entity.AddOnServices = updateDto.AddOnServices;

            if (updateDto.ModifiedBy != null) entity.ModifiedBy = updateDto.ModifiedBy;
            if (updateDto.IsActive.HasValue) entity.IsActive = updateDto.IsActive.Value;

            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteArticleAsync(Guid id)
        {
            var entity = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return false;
            _context.Articles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static ArticleResponseDto MapToDto(Article a) => new ArticleResponseDto
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            Category = a.Category,
            Type = a.Type,
            Status = a.Status,
            Tags = a.Tags,
            Notes = a.Notes,
            SKU = a.SKU,
            Stock = a.Stock,
            MinStock = a.MinStock,
            CostPrice = a.CostPrice,
            SellPrice = a.SellPrice,
            Supplier = a.Supplier,
            Location = a.Location,
            SubLocation = a.SubLocation,
            BasePrice = a.BasePrice,
            Duration = a.Duration,
            SkillsRequired = a.SkillsRequired,
            MaterialsNeeded = a.MaterialsNeeded,
            PreferredUsers = a.PreferredUsers,
            HourlyRate = a.HourlyRate,
            EstimatedDuration = a.EstimatedDuration,
            MaterialsIncluded = a.MaterialsIncluded,
            WarrantyCoverage = a.WarrantyCoverage,
            ServiceArea = a.ServiceArea,
            Inclusions = a.Inclusions,
            AddOnServices = a.AddOnServices,
            LastUsed = a.LastUsed,
            LastUsedBy = a.LastUsedBy,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            CreatedBy = a.CreatedBy,
            ModifiedBy = a.ModifiedBy,
            IsActive = a.IsActive
        };
    }
}