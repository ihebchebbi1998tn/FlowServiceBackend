using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Lookups.Models
{
    [Table("Currencies")]
    public class Currency
    {
        [Key]
        [MaxLength(3)]
        public string Id { get; set; } = string.Empty; // ISO currency code (USD, EUR, etc.)

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [MaxLength(3)]
        public string Code { get; set; } = string.Empty; // ISO currency code

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        [Required]
        [MaxLength(100)]
        public string CreatedUser { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ModifyUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
