using MyApi.Data;
using MyApi.Modules.Users.DTOs;
using MyApi.Modules.Users.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MyApi.Modules.Users.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserListResponseDto> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => !u.IsDeleted)
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

                var userDtos = users.Select(u => MapToUserDto(u)).ToList();

                return new UserListResponseDto
                {
                    Users = userDtos,
                    TotalCount = userDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id {UserId}", id);
                throw;
            }
        }

        public async Task<UserResponseDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                throw;
            }
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto createDto, string createdByUser)
        {
            try
            {
                // Check if email exists in Users table
                var existsInUsers = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == createDto.Email.ToLower() && !u.IsDeleted);

                if (existsInUsers)
                {
                    throw new InvalidOperationException("A user with this email already exists in the Users table");
                }

                // Check if email exists in MainAdminUsers table
                var existsInAdminUsers = await _context.MainAdminUsers
                    .AnyAsync(u => u.Email.ToLower() == createDto.Email.ToLower());

                if (existsInAdminUsers)
                {
                    throw new InvalidOperationException("A user with this email already exists in the Admin Users table");
                }

                // Hash password
                var passwordHash = HashPassword(createDto.Password);

                var user = new User
                {
                    Email = createDto.Email.ToLower(),
                    PasswordHash = passwordHash,
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    PhoneNumber = createDto.PhoneNumber,
                    Country = createDto.Country,
                    Role = createDto.Role,
                    CreatedUser = createdByUser,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully with ID {UserId}", user.Id);
                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<UserResponseDto?> UpdateUserAsync(int id, UpdateRegularUserRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                // Check if email is being changed and if new email already exists
                if (!string.IsNullOrEmpty(updateDto.Email) && 
                    updateDto.Email.ToLower() != user.Email.ToLower())
                {
                    // Check if new email exists in Users table
                    var existsInUsers = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == updateDto.Email.ToLower() && !u.IsDeleted && u.Id != id);

                    if (existsInUsers)
                    {
                        throw new InvalidOperationException("A user with this email already exists in the Users table");
                    }

                    // Check if new email exists in MainAdminUsers table
                    var existsInAdminUsers = await _context.MainAdminUsers
                        .AnyAsync(u => u.Email.ToLower() == updateDto.Email.ToLower());

                    if (existsInAdminUsers)
                    {
                        throw new InvalidOperationException("A user with this email already exists in the Admin Users table");
                    }

                    user.Email = updateDto.Email.ToLower();
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.FirstName))
                    user.FirstName = updateDto.FirstName;

                if (!string.IsNullOrEmpty(updateDto.LastName))
                    user.LastName = updateDto.LastName;

                if (updateDto.PhoneNumber != null)
                    user.PhoneNumber = updateDto.PhoneNumber;

                if (!string.IsNullOrEmpty(updateDto.Country))
                    user.Country = updateDto.Country;

                if (updateDto.Role != null)
                    user.Role = updateDto.Role;

                if (updateDto.IsActive.HasValue)
                    user.IsActive = updateDto.IsActive.Value;

                user.ModifyUser = modifiedByUser;
                user.ModifyDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully with ID {UserId}", user.Id);
                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id, string deletedByUser)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return false;
                }

                // Soft delete
                user.IsDeleted = true;
                user.ModifyUser = deletedByUser;
                user.ModifyDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User soft deleted successfully with ID {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                throw;
            }
        }

        public async Task<bool> ChangeUserPasswordAsync(int id, string newPassword, string modifiedByUser)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return false;
                }

                user.PasswordHash = HashPassword(newPassword);
                user.ModifyUser = modifiedByUser;
                user.ModifyDate = DateTime.UtcNow;

                // Clear existing tokens to force re-login
                user.AccessToken = null;
                user.RefreshToken = null;
                user.TokenExpiresAt = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user ID {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user ID {UserId}", id);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            try
            {
                // Check both Users and MainAdminUsers tables for email existence
                var existsInUsers = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);

                var existsInAdminUsers = await _context.MainAdminUsers
                    .AnyAsync(u => u.Email.ToLower() == email.ToLower());

                return existsInUsers || existsInAdminUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists with email {Email}", email);
                throw;
            }
        }

        private static UserResponseDto MapToUserDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedUser = user.CreatedUser,
                ModifyUser = user.ModifyUser,
                CreatedDate = user.CreatedDate,
                ModifyDate = user.ModifyDate,
                LastLoginAt = user.LastLoginAt
            };
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }
    }
}
