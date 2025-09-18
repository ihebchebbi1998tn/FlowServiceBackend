using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Services.Users
{
    public class UserManagementService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(ApplicationDbContext context, ILogger<UserManagementService> logger)
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
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Position = u.Position,
                        Department = u.Department,
                        IsActive = u.IsActive,
                        CreatedDate = u.CreatedDate,
                        ModifiedDate = u.ModifiedDate
                    })
                    .ToListAsync();

                return new UserListResponseDto
                {
                    Users = users,
                    TotalCount = users.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                throw;
            }
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && !u.IsDeleted)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Position = u.Position,
                        Department = u.Department,
                        IsActive = u.IsActive,
                        CreatedDate = u.CreatedDate,
                        ModifiedDate = u.ModifiedDate
                    })
                    .FirstOrDefaultAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with id {id}");
                throw;
            }
        }

        public async Task<UserResponseDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email == email && !u.IsDeleted)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Position = u.Position,
                        Department = u.Department,
                        IsActive = u.IsActive,
                        CreatedDate = u.CreatedDate,
                        ModifiedDate = u.ModifiedDate
                    })
                    .FirstOrDefaultAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with email {email}");
                throw;
            }
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto createDto, string createdByUser)
        {
            try
            {
                var newUser = new User
                {
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    Email = createDto.Email,
                    PhoneNumber = createDto.PhoneNumber,
                    Position = createDto.Position,
                    Department = createDto.Department,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdByUser,
                    IsActive = createDto.IsActive
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return new UserResponseDto
                {
                    Id = newUser.Id,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    Email = newUser.Email,
                    PhoneNumber = newUser.PhoneNumber,
                    Position = newUser.Position,
                    Department = newUser.Department,
                    IsActive = newUser.IsActive,
                    CreatedDate = newUser.CreatedDate,
                    ModifiedDate = newUser.ModifiedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user with email {createDto.Email}");
                throw;
            }
        }

        public async Task<UserResponseDto?> UpdateUserAsync(int id, UpdateRegularUserRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(id);

                if (existingUser == null || existingUser.IsDeleted)
                {
                    return null;
                }

                existingUser.FirstName = updateDto.FirstName;
                existingUser.LastName = updateDto.LastName;
                existingUser.PhoneNumber = updateDto.PhoneNumber;
                existingUser.Position = updateDto.Position;
                existingUser.Department = updateDto.Department;
                existingUser.ModifiedDate = DateTime.UtcNow;
                existingUser.ModifiedBy = modifiedByUser;
                existingUser.IsActive = updateDto.IsActive;

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return new UserResponseDto
                {
                    Id = existingUser.Id,
                    FirstName = existingUser.FirstName,
                    LastName = existingUser.LastName,
                    Email = existingUser.Email,
                    PhoneNumber = existingUser.PhoneNumber,
                    Position = existingUser.Position,
                    Department = existingUser.Department,
                    IsActive = existingUser.IsActive,
                    CreatedDate = existingUser.CreatedDate,
                    ModifiedDate = existingUser.ModifiedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user with id {id}");
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id, string deletedByUser)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(id);

                if (existingUser == null || existingUser.IsDeleted)
                {
                    return false;
                }

                existingUser.IsDeleted = true;
                existingUser.DeletedDate = DateTime.UtcNow;
                existingUser.DeletedBy = deletedByUser;

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user with id {id}");
                throw;
            }
        }

        public async Task<bool> ChangeUserPasswordAsync(int id, string newPassword, string modifiedByUser)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(id);

                if (existingUser == null || existingUser.IsDeleted)
                {
                    return false;
                }

                // Hash the new password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                existingUser.Password = passwordHash;
                existingUser.ModifiedDate = DateTime.UtcNow;
                existingUser.ModifiedBy = modifiedByUser;

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for user with id {id}");
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Email == email && !u.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user exists with email {email}");
                throw;
            }
        }
    }
}
