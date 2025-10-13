using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Lookups.DTOs
{
    public class CreatePreferencesRequest
    {
        [Required]
        public string Theme { get; set; } = "system";
        
        [Required]
        public string Language { get; set; } = "en";
        
        [Required]
        public string PrimaryColor { get; set; } = "blue";
        
        [Required]
        public string LayoutMode { get; set; } = "sidebar";
        
        [Required]
        public string DataView { get; set; } = "table";
        
        public string? Timezone { get; set; }
        public string DateFormat { get; set; } = "MM/DD/YYYY";
        public string TimeFormat { get; set; } = "12h";
        public string Currency { get; set; } = "USD";
        public string NumberFormat { get; set; } = "comma";
        public string? Notifications { get; set; } = "{}";
        public bool? SidebarCollapsed { get; set; }
        public bool? CompactMode { get; set; }
        public bool? ShowTooltips { get; set; }
        public bool? AnimationsEnabled { get; set; }
        public bool? SoundEnabled { get; set; }
        public bool? AutoSave { get; set; }
        public string? WorkArea { get; set; }
        public string? DashboardLayout { get; set; }
        public string? QuickAccessItems { get; set; }
    }

    public class UpdatePreferencesRequest
    {
        public string? Theme { get; set; }
        public string? Language { get; set; }
        public string? PrimaryColor { get; set; }
        public string? LayoutMode { get; set; }
        public string? DataView { get; set; }
        public string? Timezone { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? Currency { get; set; }
        public string? NumberFormat { get; set; }
        public string? Notifications { get; set; }
        public bool? SidebarCollapsed { get; set; }
        public bool? CompactMode { get; set; }
        public bool? ShowTooltips { get; set; }
        public bool? AnimationsEnabled { get; set; }
        public bool? SoundEnabled { get; set; }
        public bool? AutoSave { get; set; }
        public string? WorkArea { get; set; }
        public string? DashboardLayout { get; set; }
        public string? QuickAccessItems { get; set; }
    }

    public class PreferencesResponse
    {
        public required string Id { get; set; }
        public required int UserId { get; set; }
        public required string Theme { get; set; }
        public required string Language { get; set; }
        public required string PrimaryColor { get; set; }
        public required string LayoutMode { get; set; }
        public required string DataView { get; set; }
        public string? Timezone { get; set; }
        public required string DateFormat { get; set; }
        public required string TimeFormat { get; set; }
        public required string Currency { get; set; }
        public required string NumberFormat { get; set; }
        public string? Notifications { get; set; }
        public bool SidebarCollapsed { get; set; }
        public bool CompactMode { get; set; }
        public bool ShowTooltips { get; set; }
        public bool AnimationsEnabled { get; set; }
        public bool SoundEnabled { get; set; }
        public bool AutoSave { get; set; }
        public string? WorkArea { get; set; }
        public string? DashboardLayout { get; set; }
        public string? QuickAccessItems { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
