using MyApi.DTOs;

namespace MyApi.Services
{
    public interface IUserService
    {
        Task<UserListResponseDto> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto?> GetUserByEmailAsync(string email);
        Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto createDto, string createdByUser);
        Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteUserAsync(int id, string deletedByUser);
        Task<bool> ChangeUserPasswordAsync(int id, string newPassword, string modifiedByUser);
        Task<bool> UserExistsAsync(string email);
    }
}