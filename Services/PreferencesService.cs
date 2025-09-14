using FlowServiceBackend.Data;
using FlowServiceBackend.DTOs;
using FlowServiceBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowServiceBackend.Services
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
                    Theme = request.Theme,
                    Language = request.Language,
                    PrimaryColor = request.PrimaryColor,
                    LayoutMode = request.LayoutMode,
                    DataView = request.DataView,
                    Timezone = request.Timezone,
                    DateFormat = request.DateFormat,
                    TimeFormat = request.TimeFormat,
                    Currency = request.Currency,
                    NumberFormat = request.NumberFormat,
                    Notifications = request.Notifications ?? "{}",
                    SidebarCollapsed = request.SidebarCollapsed ?? false,
                    CompactMode = request.CompactMode ?? false,
                    ShowTooltips = request.ShowTooltips ?? true,
                    AnimationsEnabled = request.AnimationsEnabled ?? true,
                    SoundEnabled = request.SoundEnabled ?? true,
                    AutoSave = request.AutoSave ?? true,
                    WorkArea = request.WorkArea,
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

                preferences.Theme = request.Theme;
                preferences.Language = request.Language;
                preferences.PrimaryColor = request.PrimaryColor;
                preferences.LayoutMode = request.LayoutMode;
                preferences.DataView = request.DataView;
                preferences.Timezone = request.Timezone;
                preferences.DateFormat = request.DateFormat;
                preferences.TimeFormat = request.TimeFormat;
                preferences.Currency = request.Currency;
                preferences.NumberFormat = request.NumberFormat;
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
    }
}