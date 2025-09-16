using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;

namespace MyApi.Services
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _context;

        public RoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Where(r => !r.IsDeleted)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    IsActive = r.IsActive,
                    UserCount = r.UserRoles.Count(ur => ur.IsActive)
                })
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<RoleDto?> GetRoleByIdAsync(int id)
        {
            var role = await _context.Roles
                .Where(r => r.Id == id && !r.IsDeleted)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    IsActive = r.IsActive,
                    UserCount = r.UserRoles.Count(ur => ur.IsActive)
                })
                .FirstOrDefaultAsync();

            return role;
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, string createdBy)
        {
            var role = new Role
            {
                Name = request.Name,
                Description = request.Description,
                CreatedUser = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                IsActive = role.IsActive,
                UserCount = 0
            };
        }

        public async Task<RoleDto> UpdateRoleAsync(int id, UpdateRoleRequest request, string modifiedBy)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (role == null)
                throw new ArgumentException("Role not found");

            role.Name = request.Name;
            role.Description = request.Description;
            role.IsActive = request.IsActive;
            role.ModifyUser = modifiedBy;
            role.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                IsActive = role.IsActive,
                UserCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == id && ur.IsActive)
            };
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (role == null)
                return false;

            // Soft delete
            role.IsDeleted = true;
            role.UpdatedAt = DateTime.UtcNow;

            // Deactivate all user role assignments
            var userRoles = await _context.UserRoles
                .Where(ur => ur.RoleId == id)
                .ToListAsync();

            foreach (var userRole in userRoles)
            {
                userRole.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RoleExistsAsync(string name, int? excludeId = null)
        {
            var query = _context.Roles.Where(r => r.Name.ToLower() == name.ToLower() && !r.IsDeleted);
            
            if (excludeId.HasValue)
                query = query.Where(r => r.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId, string assignedBy)
        {
            // Check if assignment already exists
            var existingAssignment = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingAssignment != null)
            {
                existingAssignment.IsActive = true;
                existingAssignment.AssignedAt = DateTime.UtcNow;
                existingAssignment.AssignedBy = assignedBy;
            }
            else
            {
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedBy = assignedBy,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UserRoles.Add(userRole);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                return false;

            userRole.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RoleDto>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.IsActive)
                .Include(ur => ur.Role)
                .Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description,
                    CreatedAt = ur.Role.CreatedAt,
                    UpdatedAt = ur.Role.UpdatedAt,
                    IsActive = ur.Role.IsActive,
                    UserCount = ur.Role.UserRoles.Count(r => r.IsActive)
                })
                .ToListAsync();
        }
    }
}