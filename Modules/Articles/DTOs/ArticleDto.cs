using System;
using System.Collections.Generic;

namespace MyApi.Modules.Articles.DTOs
{
    // =====================================================
    // Article DTOs
    // =====================================================

    public class ArticleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Material-specific
        public int? Stock { get; set; }
        public int? MinStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public string? Supplier { get; set; }
        public string? Location { get; set; }
        public string? SubLocation { get; set; }

        // Service-specific
        public decimal? BasePrice { get; set; }
        public int? Duration { get; set; }
        public List<string>? SkillsRequired { get; set; }
        public List<string>? MaterialsNeeded { get; set; }
        public List<string>? PreferredUsers { get; set; }

        // Usage tracking
        public DateTime? LastUsed { get; set; }
        public string? LastUsedBy { get; set; }

        // Common
        public List<string>? Tags { get; set; }
        public string? Notes { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }

    public class CreateArticleDto
    {
        public string Type { get; set; } = string.Empty; // 'material' or 'service'
        public string Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Material-specific
        public int? Stock { get; set; }
        public int? MinStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public string? Supplier { get; set; }
        public string? Location { get; set; }
        public string? SubLocation { get; set; }

        // Service-specific
        public decimal? BasePrice { get; set; }
        public int? Duration { get; set; }
        public List<string>? SkillsRequired { get; set; }
        public List<string>? MaterialsNeeded { get; set; }
        public List<string>? PreferredUsers { get; set; }

        // Common
        public List<string>? Tags { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateArticleDto
    {
        public string? Name { get; set; }
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }

        // Material-specific
        public int? Stock { get; set; }
        public int? MinStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public string? Supplier { get; set; }
        public string? Location { get; set; }
        public string? SubLocation { get; set; }

        // Service-specific
        public decimal? BasePrice { get; set; }
        public int? Duration { get; set; }
        public List<string>? SkillsRequired { get; set; }
        public List<string>? MaterialsNeeded { get; set; }
        public List<string>? PreferredUsers { get; set; }

        // Common
        public List<string>? Tags { get; set; }
        public string? Notes { get; set; }
    }

    public class ArticleListDto
    {
        public List<ArticleDto> Data { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

    public class PaginationDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Pages { get; set; }
    }

    // =====================================================
    // Category DTOs
    // =====================================================

    public class ArticleCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateArticleCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public int Order { get; set; }
    }

    // =====================================================
    // Location DTOs
    // =====================================================

    public class LocationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? AssignedTechnician { get; set; }
        public int? Capacity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLocationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? AssignedTechnician { get; set; }
        public int? Capacity { get; set; }
    }

    // =====================================================
    // Inventory Transaction DTOs
    // =====================================================

    public class InventoryTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string ArticleId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateInventoryTransactionDto
    {
        public string ArticleId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    // =====================================================
    // Batch Operation DTOs
    // =====================================================

    public class BatchUpdateStockDto
    {
        public List<StockUpdateItem> Items { get; set; } = new();
    }

    public class StockUpdateItem
    {
        public string Id { get; set; } = string.Empty;
        public int Stock { get; set; }
    }

    public class BatchOperationResultDto
    {
        public bool Success { get; set; }
        public int Updated { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
