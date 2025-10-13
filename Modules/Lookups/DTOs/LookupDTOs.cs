using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Lookups.DTOs
{
    // Response DTOs
    public class LookupItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedUser { get; set; } = string.Empty;
        public string? ModifyUser { get; set; }
        
        // Type-specific properties
        public int? Level { get; set; } // for priorities
        public bool? IsCompleted { get; set; } // for statuses
        public int? DefaultDuration { get; set; } // for event types
        public bool? IsAvailable { get; set; } // for technician statuses
        public bool? IsPaid { get; set; } // for leave types
        public string? Category { get; set; } // for skills
    }

    public class LookupListResponseDto
    {
        public List<LookupItemDto> items { get; set; } = new List<LookupItemDto>();
        public int totalCount { get; set; }
    }

    public class CurrencyDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedUser { get; set; } = string.Empty;
        public string? ModifyUser { get; set; }
    }

    public class CurrencyListResponseDto
    {
        public List<CurrencyDto> currencies { get; set; } = new List<CurrencyDto>();
        public int totalCount { get; set; }
    }

    // Request DTOs
    public class CreateLookupItemRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        // Type-specific properties
        public int? Level { get; set; }
        public bool? IsCompleted { get; set; }
        public int? DefaultDuration { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsPaid { get; set; }
        
        [StringLength(100)]
        public string? Category { get; set; }
    }

    public class UpdateLookupItemRequestDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? Color { get; set; }

        public bool? IsActive { get; set; }

        public int? SortOrder { get; set; }

        // Type-specific properties
        public int? Level { get; set; }
        public bool? IsCompleted { get; set; }
        public int? DefaultDuration { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsPaid { get; set; }
        
        [StringLength(100)]
        public string? Category { get; set; }
    }

    public class CreateCurrencyRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public int SortOrder { get; set; } = 0;
    }

    public class UpdateCurrencyRequestDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(10)]
        public string? Symbol { get; set; }

        [StringLength(3)]
        public string? Code { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDefault { get; set; }

        public int? SortOrder { get; set; }
    }
}
