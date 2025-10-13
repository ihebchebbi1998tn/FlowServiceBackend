using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Modules.Auth.Models;

namespace MyApi.Modules.Users.Models
{
    [Table("UserPreferences")]
    public class UserPreferences
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public required int UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Theme { get; set; } = "system"; // light, dark, system

        [Required]
        [MaxLength(5)]
        public string Language { get; set; } = "en";

        [Required]
        [MaxLength(20)]
        public string PrimaryColor { get; set; } = "blue"; // blue, red, green, purple, orange, indigo

        [Required]
        [MaxLength(20)]
        public string LayoutMode { get; set; } = "sidebar"; // sidebar, topbar

        [Required]
        [MaxLength(10)]
        public string DataView { get; set; } = "table"; // table, list, grid

        [MaxLength(100)]
        public string? Timezone { get; set; }

        [Required]
        [MaxLength(20)]
        public string DateFormat { get; set; } = "MM/DD/YYYY";

        [Required]
        [MaxLength(5)]
        public string TimeFormat { get; set; } = "12h"; // 12h, 24h

        [Required]
        [MaxLength(5)]
        public string Currency { get; set; } = "USD";

        [Required]
        [MaxLength(10)]
        public string NumberFormat { get; set; } = "comma"; // comma, dot

        [Column(TypeName = "nvarchar(max)")]
        public string? Notifications { get; set; } = "{}";

        public bool SidebarCollapsed { get; set; } = false;

        public bool CompactMode { get; set; } = false;

        public bool ShowTooltips { get; set; } = true;

        public bool AnimationsEnabled { get; set; } = true;

        public bool SoundEnabled { get; set; } = true;

        public bool AutoSave { get; set; } = true;

        [MaxLength(100)]
        public string? WorkArea { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? DashboardLayout { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? QuickAccessItems { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual MainAdminUser? User { get; set; }
    }
}
