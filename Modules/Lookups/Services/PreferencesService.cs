using MyApi.Data;
using MyApi.Modules.Lookups.DTOs;
using MyApi.Modules.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Lookups.Services
{
    public class PreferencesService : IPreferencesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PreferencesService> _logger;

        public PreferencesService(ApplicationDbContext context, ILogger<PreferencesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PreferencesResponse?> GetUserPreferencesAsync(string userId)
        {
            try
            {
                if (!int.TryParse(userId, out int userIdInt))
                    return null;
                    
                var preferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userIdInt);

                if (preferences == null)
                    return null;

                return new PreferencesResponse
                {
                    Id = preferences.Id,
                    UserId = preferences.UserId,
                    Theme = preferences.Theme,
                    Language = preferences.Language,
                    PrimaryColor = preferences.PrimaryColor,
                    LayoutMode = preferences.LayoutMode,
                    DataView = preferences.DataView,
                    Timezone = preferences.Timezone,
                    DateFormat = preferences.DateFormat,
                    TimeFormat = preferences.TimeFormat,
                    Currency = preferences.Currency,
                    NumberFormat = preferences.NumberFormat,
                    Notifications = preferences.Notifications,
                    SidebarCollapsed = preferences.SidebarCollapsed,
                    CompactMode = preferences.CompactMode,
                    ShowTooltips = preferences.ShowTooltips,
                    AnimationsEnabled = preferences.AnimationsEnabled,
                    SoundEnabled = preferences.SoundEnabled,
                    AutoSave = preferences.AutoSave,
                    WorkArea = preferences.WorkArea,
                    DashboardLayout = preferences.DashboardLayout,
                    QuickAccessItems = preferences.QuickAccessItems,
                    CreatedAt = preferences.CreatedAt,
                    UpdatedAt = preferences.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user preferences for user {UserId}", userId);
                throw;
            }
        }

        public async Task<PreferencesResponse> CreateUserPreferencesAsync(string userId, CreatePreferencesRequest request)
        {
            try
            {
                if (!int.TryParse(userId, out int userIdInt))
                    throw new ArgumentException("Invalid user ID format");
                    
                // Check if preferences already exist
                var existingPreferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userIdInt);

                if (existingPreferences != null)
                {
                    throw new InvalidOperationException("User preferences already exist. Use update instead.");
                }

                var preferences = new UserPreferences
                {
                    UserId = userIdInt,
                    Theme = ValidateAndDefault(request.Theme, "system", 20),
                    Language = ValidateAndDefault(request.Language, "en", 5),
                    PrimaryColor = ValidateAndDefault(request.PrimaryColor, "blue", 20),
                    LayoutMode = ValidateAndDefault(request.LayoutMode, "sidebar", 20),
                    DataView = ValidateAndDefault(request.DataView, "table", 10),
                    Timezone = ValidateString(request.Timezone, 100),
                    DateFormat = ValidateAndDefault(request.DateFormat, "MM/DD/YYYY", 20),
                    TimeFormat = ValidateAndDefault(request.TimeFormat, "12h", 5),
                    Currency = ValidateAndDefault(request.Currency, "USD", 5),
                    NumberFormat = ValidateAndDefault(request.NumberFormat, "comma", 10),
                    Notifications = request.Notifications ?? "{}",
                    SidebarCollapsed = request.SidebarCollapsed ?? false,
                    CompactMode = request.CompactMode ?? false,
                    ShowTooltips = request.ShowTooltips ?? true,
                    AnimationsEnabled = request.AnimationsEnabled ?? true,
                    SoundEnabled = request.SoundEnabled ?? true,
                    AutoSave = request.AutoSave ?? true,
                    WorkArea = ValidateString(request.WorkArea, 100),
                    DashboardLayout = request.DashboardLayout,
                    QuickAccessItems = request.QuickAccessItems ?? "[]",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserPreferences.Add(preferences);
                await _context.SaveChangesAsync();

                return new PreferencesResponse
                {
                    Id = preferences.Id,
                    UserId = preferences.UserId,
                    Theme = preferences.Theme,
                    Language = preferences.Language,
                    PrimaryColor = preferences.PrimaryColor,
                    LayoutMode = preferences.LayoutMode,
                    DataView = preferences.DataView,
                    Timezone = preferences.Timezone,
                    DateFormat = preferences.DateFormat,
                    TimeFormat = preferences.TimeFormat,
                    Currency = preferences.Currency,
                    NumberFormat = preferences.NumberFormat,
                    Notifications = preferences.Notifications,
                    SidebarCollapsed = preferences.SidebarCollapsed,
                    CompactMode = preferences.CompactMode,
                    ShowTooltips = preferences.ShowTooltips,
                    AnimationsEnabled = preferences.AnimationsEnabled,
                    SoundEnabled = preferences.SoundEnabled,
                    AutoSave = preferences.AutoSave,
                    WorkArea = preferences.WorkArea,
                    DashboardLayout = preferences.DashboardLayout,
                    QuickAccessItems = preferences.QuickAccessItems,
                    CreatedAt = preferences.CreatedAt,
                    UpdatedAt = preferences.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user preferences for user {UserId}", userId);
                throw;
            }
        }

        public async Task<PreferencesResponse?> UpdateUserPreferencesAsync(string userId, UpdatePreferencesRequest request)
        {
            try
            {
                if (!int.TryParse(userId, out int userIdInt))
                    return null;
                    
                var preferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userIdInt);

                if (preferences == null)
                    return null;

                preferences.Theme = ValidateAndDefault(request.Theme, preferences.Theme, 20);
                preferences.Language = ValidateAndDefault(request.Language, preferences.Language, 5);
                preferences.PrimaryColor = ValidateAndDefault(request.PrimaryColor, preferences.PrimaryColor, 20);
                preferences.LayoutMode = ValidateAndDefault(request.LayoutMode, preferences.LayoutMode, 20);
                preferences.DataView = ValidateAndDefault(request.DataView, preferences.DataView, 10);
                preferences.Timezone = ValidateString(request.Timezone, 100) ?? preferences.Timezone;
                preferences.DateFormat = ValidateAndDefault(request.DateFormat, preferences.DateFormat, 20);
                preferences.TimeFormat = ValidateAndDefault(request.TimeFormat, preferences.TimeFormat, 5);
                preferences.Currency = ValidateAndDefault(request.Currency, preferences.Currency, 5);
                preferences.NumberFormat = ValidateAndDefault(request.NumberFormat, preferences.NumberFormat, 10);
                preferences.Notifications = request.Notifications ?? preferences.Notifications;
                preferences.SidebarCollapsed = request.SidebarCollapsed ?? preferences.SidebarCollapsed;
                preferences.CompactMode = request.CompactMode ?? preferences.CompactMode;
                preferences.ShowTooltips = request.ShowTooltips ?? preferences.ShowTooltips;
                preferences.AnimationsEnabled = request.AnimationsEnabled ?? preferences.AnimationsEnabled;
                preferences.SoundEnabled = request.SoundEnabled ?? preferences.SoundEnabled;
                preferences.AutoSave = request.AutoSave ?? preferences.AutoSave;
                preferences.WorkArea = request.WorkArea ?? preferences.WorkArea;
                preferences.DashboardLayout = request.DashboardLayout ?? preferences.DashboardLayout;
                preferences.QuickAccessItems = request.QuickAccessItems ?? preferences.QuickAccessItems;
                preferences.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new PreferencesResponse
                {
                    Id = preferences.Id,
                    UserId = preferences.UserId,
                    Theme = preferences.Theme,
                    Language = preferences.Language,
                    PrimaryColor = preferences.PrimaryColor,
                    LayoutMode = preferences.LayoutMode,
                    DataView = preferences.DataView,
                    Timezone = preferences.Timezone,
                    DateFormat = preferences.DateFormat,
                    TimeFormat = preferences.TimeFormat,
                    Currency = preferences.Currency,
                    NumberFormat = preferences.NumberFormat,
                    Notifications = preferences.Notifications,
                    SidebarCollapsed = preferences.SidebarCollapsed,
                    CompactMode = preferences.CompactMode,
                    ShowTooltips = preferences.ShowTooltips,
                    AnimationsEnabled = preferences.AnimationsEnabled,
                    SoundEnabled = preferences.SoundEnabled,
                    AutoSave = preferences.AutoSave,
                    WorkArea = preferences.WorkArea,
                    DashboardLayout = preferences.DashboardLayout,
                    QuickAccessItems = preferences.QuickAccessItems,
                    CreatedAt = preferences.CreatedAt,
                    UpdatedAt = preferences.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserPreferencesAsync(string userId)
        {
            try
            {
                if (!int.TryParse(userId, out int userIdInt))
                    return false;
                    
                var preferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userIdInt);

                if (preferences == null)
                    return false;

                _context.UserPreferences.Remove(preferences);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user preferences for user {UserId}", userId);
                throw;
            }
        }

        private static string ValidateAndDefault(string? value, string defaultValue, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            
            return value.Length > maxLength ? defaultValue : value;
        }

        private static string? ValidateString(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }
    }
}
