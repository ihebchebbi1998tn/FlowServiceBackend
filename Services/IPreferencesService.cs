using MyApi.DTOs;

namespace MyApi.Services
{
    public interface IPreferencesService
    {
        Task<PreferencesResponse?> GetUserPreferencesAsync(string userId);
        Task<PreferencesResponse> CreateUserPreferencesAsync(string userId, CreatePreferencesRequest request);
        Task<PreferencesResponse?> UpdateUserPreferencesAsync(string userId, UpdatePreferencesRequest request);
        Task<bool> DeleteUserPreferencesAsync(string userId);
    }
}