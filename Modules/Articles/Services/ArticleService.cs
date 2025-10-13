using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyApi.Modules.Articles.Models;
using MyApi.Modules.Articles.DTOs;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Articles.Services
{
    public interface IArticleService
    {
        Task<ArticleListDto> GetAllArticlesAsync(string? type, string? category, string? status, string? location, string? search, int page, int limit, string? sortBy, string? sortOrder);
        Task<ArticleDto?> GetArticleByIdAsync(string id);
        Task<ArticleDto> CreateArticleAsync(CreateArticleDto dto, string userId);
        Task<ArticleDto?> UpdateArticleAsync(string id, UpdateArticleDto dto, string userId);
        Task<bool> DeleteArticleAsync(string id);
        Task<InventoryTransactionDto> CreateTransactionAsync(CreateInventoryTransactionDto dto, string userId);
        Task<List<InventoryTransactionDto>> GetArticleTransactionsAsync(string articleId);
        Task<BatchOperationResultDto> BatchUpdateStockAsync(BatchUpdateStockDto dto, string userId);
        Task<List<ArticleCategoryDto>> GetAllCategoriesAsync();
        Task<ArticleCategoryDto> CreateCategoryAsync(CreateArticleCategoryDto dto);
        Task<List<LocationDto>> GetAllLocationsAsync();
        Task<LocationDto> CreateLocationAsync(CreateLocationDto dto);
    }

    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _context;

        public ArticleService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // Article CRUD Operations
        // =====================================================

        public async Task<ArticleListDto> GetAllArticlesAsync(
            string? type = null,
            string? category = null,
            string? status = null,
            string? location = null,
            string? search = null,
            int page = 1,
            int limit = 20,
            string? sortBy = null,
            string? sortOrder = "asc")
        {
            var query = _context.Set<Article>().AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrEmpty(location))
                query = query.Where(a => a.Location == location);

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(a =>
                    a.Name.ToLower().Contains(searchLower) ||
                    (a.Sku != null && a.Sku.ToLower().Contains(searchLower)) ||
                    a.Category.ToLower().Contains(searchLower));
            }

            // Count total before pagination
            var total = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            // Apply pagination
            var articles = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return new ArticleListDto
            {
                Data = articles.Select(MapArticleToDto).ToList(),
                Pagination = new PaginationDto
                {
                    Total = total,
                    Page = page,
                    Limit = limit,
                    Pages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        public async Task<ArticleDto?> GetArticleByIdAsync(string id)
        {
            var article = await _context.Set<Article>()
                .FirstOrDefaultAsync(a => a.Id == id);

            return article != null ? MapArticleToDto(article) : null;
        }

        public async Task<ArticleDto> CreateArticleAsync(CreateArticleDto dto, string userId)
        {
            var article = new Article
            {
                Id = $"{dto.Type}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Type = dto.Type,
                Name = dto.Name,
                Sku = dto.Sku,
                Description = dto.Description,
                Category = dto.Category,
                Status = dto.Status,
                
                // Material fields
                Stock = dto.Stock,
                MinStock = dto.MinStock,
                CostPrice = dto.CostPrice,
                SellPrice = dto.SellPrice,
                Supplier = dto.Supplier,
                Location = dto.Location,
                SubLocation = dto.SubLocation,
                
                // Service fields
                BasePrice = dto.BasePrice,
                Duration = dto.Duration,
                SkillsRequired = dto.SkillsRequired != null ? JsonSerializer.Serialize(dto.SkillsRequired) : null,
                MaterialsNeeded = dto.MaterialsNeeded != null ? JsonSerializer.Serialize(dto.MaterialsNeeded) : null,
                PreferredUsers = dto.PreferredUsers != null ? JsonSerializer.Serialize(dto.PreferredUsers) : null,
                
                // Common fields
                Tags = dto.Tags != null ? JsonSerializer.Serialize(dto.Tags) : null,
                Notes = dto.Notes,
                
                // Metadata
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ModifiedBy = userId
            };

            _context.Set<Article>().Add(article);
            await _context.SaveChangesAsync();

            return MapArticleToDto(article);
        }

        public async Task<ArticleDto?> UpdateArticleAsync(string id, UpdateArticleDto dto, string userId)
        {
            var article = await _context.Set<Article>()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return null;

            // Update only provided fields
            if (dto.Name != null) article.Name = dto.Name;
            if (dto.Sku != null) article.Sku = dto.Sku;
            if (dto.Description != null) article.Description = dto.Description;
            if (dto.Category != null) article.Category = dto.Category;
            if (dto.Status != null) article.Status = dto.Status;

            // Material fields
            if (dto.Stock.HasValue) article.Stock = dto.Stock;
            if (dto.MinStock.HasValue) article.MinStock = dto.MinStock;
            if (dto.CostPrice.HasValue) article.CostPrice = dto.CostPrice;
            if (dto.SellPrice.HasValue) article.SellPrice = dto.SellPrice;
            if (dto.Supplier != null) article.Supplier = dto.Supplier;
            if (dto.Location != null) article.Location = dto.Location;
            if (dto.SubLocation != null) article.SubLocation = dto.SubLocation;

            // Service fields
            if (dto.BasePrice.HasValue) article.BasePrice = dto.BasePrice;
            if (dto.Duration.HasValue) article.Duration = dto.Duration;
            if (dto.SkillsRequired != null) article.SkillsRequired = JsonSerializer.Serialize(dto.SkillsRequired);
            if (dto.MaterialsNeeded != null) article.MaterialsNeeded = JsonSerializer.Serialize(dto.MaterialsNeeded);
            if (dto.PreferredUsers != null) article.PreferredUsers = JsonSerializer.Serialize(dto.PreferredUsers);

            // Common fields
            if (dto.Tags != null) article.Tags = JsonSerializer.Serialize(dto.Tags);
            if (dto.Notes != null) article.Notes = dto.Notes;

            // Update metadata
            article.UpdatedAt = DateTime.UtcNow;
            article.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return MapArticleToDto(article);
        }

        public async Task<bool> DeleteArticleAsync(string id)
        {
            var article = await _context.Set<Article>()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return false;

            _context.Set<Article>().Remove(article);
            await _context.SaveChangesAsync();

            return true;
        }

        // =====================================================
        // Inventory Transactions
        // =====================================================

        public async Task<InventoryTransactionDto> CreateTransactionAsync(CreateInventoryTransactionDto dto, string userId)
        {
            var transaction = new InventoryTransaction
            {
                Id = $"TXN-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                ArticleId = dto.ArticleId,
                Type = dto.Type,
                Quantity = dto.Quantity,
                FromLocation = dto.FromLocation,
                ToLocation = dto.ToLocation,
                Reason = dto.Reason,
                Reference = dto.Reference,
                PerformedBy = userId,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<InventoryTransaction>().Add(transaction);

            // Update article stock if transaction type is 'in', 'out', or 'adjustment'
            if (dto.Type == "in" || dto.Type == "out" || dto.Type == "adjustment")
            {
                var article = await _context.Set<Article>()
                    .FirstOrDefaultAsync(a => a.Id == dto.ArticleId);

                if (article != null && article.Stock.HasValue)
                {
                    if (dto.Type == "in" || dto.Type == "adjustment")
                        article.Stock += dto.Quantity;
                    else if (dto.Type == "out")
                        article.Stock -= dto.Quantity;

                    article.LastUsed = DateTime.UtcNow;
                    article.LastUsedBy = userId;
                    article.UpdatedAt = DateTime.UtcNow;
                    article.ModifiedBy = userId;
                }
            }

            await _context.SaveChangesAsync();

            return MapTransactionToDto(transaction);
        }

        public async Task<List<InventoryTransactionDto>> GetArticleTransactionsAsync(string articleId)
        {
            var transactions = await _context.Set<InventoryTransaction>()
                .Where(t => t.ArticleId == articleId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return transactions.Select(MapTransactionToDto).ToList();
        }

        // =====================================================
        // Batch Operations
        // =====================================================

        public async Task<BatchOperationResultDto> BatchUpdateStockAsync(BatchUpdateStockDto dto, string userId)
        {
            var result = new BatchOperationResultDto { Success = true };
            var errors = new List<string>();

            foreach (var item in dto.Items)
            {
                try
                {
                    var article = await _context.Set<Article>()
                        .FirstOrDefaultAsync(a => a.Id == item.Id);

                    if (article != null)
                    {
                        article.Stock = item.Stock;
                        article.UpdatedAt = DateTime.UtcNow;
                        article.ModifiedBy = userId;
                        result.Updated++;
                    }
                    else
                    {
                        result.Failed++;
                        errors.Add($"Article {item.Id} not found");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    errors.Add($"Error updating {item.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            result.Errors = errors;
            result.Success = result.Failed == 0;

            return result;
        }

        // =====================================================
        // Categories
        // =====================================================

        public async Task<List<ArticleCategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Set<ArticleCategory>()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Order)
                .ToListAsync();

            return categories.Select(MapCategoryToDto).ToList();
        }

        public async Task<ArticleCategoryDto> CreateCategoryAsync(CreateArticleCategoryDto dto)
        {
            var category = new ArticleCategory
            {
                Id = $"CAT-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Name = dto.Name,
                Type = dto.Type,
                Description = dto.Description,
                ParentId = dto.ParentId,
                Order = dto.Order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ArticleCategory>().Add(category);
            await _context.SaveChangesAsync();

            return MapCategoryToDto(category);
        }

        // =====================================================
        // Locations
        // =====================================================

        public async Task<List<LocationDto>> GetAllLocationsAsync()
        {
            var locations = await _context.Set<Location>()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return locations.Select(MapLocationToDto).ToList();
        }

        public async Task<LocationDto> CreateLocationAsync(CreateLocationDto dto)
        {
            var location = new Location
            {
                Id = $"LOC-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Name = dto.Name,
                Type = dto.Type,
                Address = dto.Address,
                AssignedTechnician = dto.AssignedTechnician,
                Capacity = dto.Capacity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<Location>().Add(location);
            await _context.SaveChangesAsync();

            return MapLocationToDto(location);
        }

        // =====================================================
        // Helper Methods
        // =====================================================

        private IQueryable<Article> ApplySorting(IQueryable<Article> query, string? sortBy, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
                "category" => isDescending ? query.OrderByDescending(a => a.Category) : query.OrderBy(a => a.Category),
                "stock" => isDescending ? query.OrderByDescending(a => a.Stock) : query.OrderBy(a => a.Stock),
                "price" => isDescending ? query.OrderByDescending(a => a.SellPrice ?? a.BasePrice) : query.OrderBy(a => a.SellPrice ?? a.BasePrice),
                "createdat" => isDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };
        }

        private ArticleDto MapArticleToDto(Article article)
        {
            return new ArticleDto
            {
                Id = article.Id,
                Type = article.Type,
                Name = article.Name,
                Sku = article.Sku,
                Description = article.Description,
                Category = article.Category,
                Status = article.Status,
                
                // Material fields
                Stock = article.Stock,
                MinStock = article.MinStock,
                CostPrice = article.CostPrice,
                SellPrice = article.SellPrice,
                Supplier = article.Supplier,
                Location = article.Location,
                SubLocation = article.SubLocation,
                
                // Service fields
                BasePrice = article.BasePrice,
                Duration = article.Duration,
                SkillsRequired = DeserializeList(article.SkillsRequired),
                MaterialsNeeded = DeserializeList(article.MaterialsNeeded),
                PreferredUsers = DeserializeList(article.PreferredUsers),
                
                // Usage tracking
                LastUsed = article.LastUsed,
                LastUsedBy = article.LastUsedBy,
                
                // Common fields
                Tags = DeserializeList(article.Tags),
                Notes = article.Notes,
                
                // Metadata
                CreatedAt = article.CreatedAt,
                UpdatedAt = article.UpdatedAt,
                CreatedBy = article.CreatedBy,
                ModifiedBy = article.ModifiedBy
            };
        }

        private ArticleCategoryDto MapCategoryToDto(ArticleCategory category)
        {
            return new ArticleCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Type = category.Type,
                Description = category.Description,
                ParentId = category.ParentId,
                Order = category.Order,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };
        }

        private LocationDto MapLocationToDto(Location location)
        {
            return new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Type = location.Type,
                Address = location.Address,
                AssignedTechnician = location.AssignedTechnician,
                Capacity = location.Capacity,
                IsActive = location.IsActive,
                CreatedAt = location.CreatedAt
            };
        }

        private InventoryTransactionDto MapTransactionToDto(InventoryTransaction transaction)
        {
            return new InventoryTransactionDto
            {
                Id = transaction.Id,
                ArticleId = transaction.ArticleId,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                FromLocation = transaction.FromLocation,
                ToLocation = transaction.ToLocation,
                Reason = transaction.Reason,
                Reference = transaction.Reference,
                PerformedBy = transaction.PerformedBy,
                Notes = transaction.Notes,
                CreatedAt = transaction.CreatedAt
            };
        }

        private List<string>? DeserializeList(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
