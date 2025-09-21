using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs
{
    public class ArticleResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Tags { get; set; }
        public string? Notes { get; set; }

        public string? SKU { get; set; }
        public int? Stock { get; set; }
        public int? MinStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public string? Supplier { get; set; }
        public string? Location { get; set; }
        public string? SubLocation { get; set; }

        public decimal? BasePrice { get; set; }
        public int? Duration { get; set; }
        public string? SkillsRequired { get; set; }
        public string? MaterialsNeeded { get; set; }
        public string? PreferredUsers { get; set; }
        public decimal? HourlyRate { get; set; }
        public string? EstimatedDuration { get; set; }
        public bool? MaterialsIncluded { get; set; }
        public string? WarrantyCoverage { get; set; }
        public string? ServiceArea { get; set; }
        public string? Inclusions { get; set; }
        public string? AddOnServices { get; set; }

        public DateTime? LastUsed { get; set; }
        public string? LastUsedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class ArticleListResponseDto
    {
        public List<ArticleResponseDto> Articles { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class CreateArticleRequestDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;
        [StringLength(4000)]
        public string? Description { get; set; }
        [Required, StringLength(100)]
        public string Category { get; set; } = string.Empty;
        [Required, StringLength(20)]
        public string Type { get; set; } = "material"; // material | service
        [Required, StringLength(50)]
        public string Status { get; set; } = "active";

        public string? Tags { get; set; }
        public string? Notes { get; set; }

        // Material
        [StringLength(50)] public string? SKU { get; set; }
        public int? Stock { get; set; }
        public int? MinStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        [StringLength(200)] public string? Supplier { get; set; }
        [StringLength(200)] public string? Location { get; set; }
        [StringLength(200)] public string? SubLocation { get; set; }

        // Service
        public decimal? BasePrice { get; set; }
        public int? Duration { get; set; }
        public string? SkillsRequired { get; set; }
        public string? MaterialsNeeded { get; set; }
        public string? PreferredUsers { get; set; }
        public decimal? HourlyRate { get; set; }
        [StringLength(100)] public string? EstimatedDuration { get; set; }
        public bool? MaterialsIncluded { get; set; }
        [StringLength(200)] public string? WarrantyCoverage { get; set; }
        [StringLength(100)] public string? ServiceArea { get; set; }
        public string? Inclusions { get; set; }
        public string? AddOnServices { get; set; }

        [Required, StringLength(100)]
        public string CreatedBy { get; set; } = "system";
    }

    public class UpdateArticleRequestDto
    {
        [StringLength(200)] public string? Name { get; set; }
        public string? Description { get; set; }
        [StringLength(100)] public string? Category { get; set; }
        [StringLength(20)] public string? Type { get; set; }
        [StringLength(50)] public string? Status { get; set; }
        public string? Tags { get; set; }
        public string? Notes { get; set; }

        // Material
        [StringLength(50)] public string? SKU { get; set; }
        public int? Stock { get; set; }
        public int? MinStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        [StringLength(200)] public string? Supplier { get; set; }
        [StringLength(200)] public string? Location { get; set; }
        [StringLength(200)] public string? SubLocation { get; set; }

        // Service
        public decimal? BasePrice { get; set; }
        public int? Duration { get; set; }
        public string? SkillsRequired { get; set; }
        public string? MaterialsNeeded { get; set; }
        public string? PreferredUsers { get; set; }
        public decimal? HourlyRate { get; set; }
        [StringLength(100)] public string? EstimatedDuration { get; set; }
        public bool? MaterialsIncluded { get; set; }
        [StringLength(200)] public string? WarrantyCoverage { get; set; }
        [StringLength(100)] public string? ServiceArea { get; set; }
        public string? Inclusions { get; set; }
        public string? AddOnServices { get; set; }

        [StringLength(100)] public string? ModifiedBy { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ArticleSearchRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? Type { get; set; } // material | service
        public string? Category { get; set; }
        public string? Status { get; set; }
        public bool? LowStockOnly { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }
}