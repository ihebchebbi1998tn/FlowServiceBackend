using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;
using System.Text.Json;

namespace MyApi.Services
{
    public class SkillService : ISkillService
    {
        private readonly ApplicationDbContext _context;

        public SkillService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SkillDto>> GetAllSkillsAsync()
        {
            return await _context.Skills
                .Where(s => !s.IsDeleted)
                .Select(s => new SkillDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Category = s.Category,
                    Level = s.Level,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    IsActive = s.IsActive,
                    UserCount = s.UserSkills.Count(us => us.IsActive)
                })
                .OrderBy(s => s.Category).ThenBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<SkillDto?> GetSkillByIdAsync(int id)
        {
            var skill = await _context.Skills
                .Where(s => s.Id == id && !s.IsDeleted)
                .Select(s => new SkillDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Category = s.Category,
                    Level = s.Level,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    IsActive = s.IsActive,
                    UserCount = s.UserSkills.Count(us => us.IsActive)
                })
                .FirstOrDefaultAsync();

            return skill;
        }

        public async Task<SkillDto> CreateSkillAsync(CreateSkillRequest request, string createdBy)
        {
            var skill = new Skill
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Level = request.Level,
                CreatedUser = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
                Category = skill.Category,
                Level = skill.Level,
                CreatedAt = skill.CreatedAt,
                UpdatedAt = skill.UpdatedAt,
                IsActive = skill.IsActive,
                UserCount = 0
            };
        }

        public async Task<SkillDto> UpdateSkillAsync(int id, UpdateSkillRequest request, string modifiedBy)
        {
            var skill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (skill == null)
                throw new ArgumentException("Skill not found");

            skill.Name = request.Name;
            skill.Description = request.Description;
            skill.Category = request.Category;
            skill.Level = request.Level;
            skill.IsActive = request.IsActive;
            skill.ModifyUser = modifiedBy;
            skill.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
                Category = skill.Category,
                Level = skill.Level,
                CreatedAt = skill.CreatedAt,
                UpdatedAt = skill.UpdatedAt,
                IsActive = skill.IsActive,
                UserCount = await _context.UserSkills.CountAsync(us => us.SkillId == id && us.IsActive)
            };
        }

        public async Task<bool> DeleteSkillAsync(int id)
        {
            var skill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (skill == null)
                return false;

            // Soft delete
            skill.IsDeleted = true;
            skill.UpdatedAt = DateTime.UtcNow;

            // Deactivate all user skill assignments
            var userSkills = await _context.UserSkills
                .Where(us => us.SkillId == id)
                .ToListAsync();

            foreach (var userSkill in userSkills)
            {
                userSkill.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SkillExistsAsync(string name, int? excludeId = null)
        {
            var query = _context.Skills.Where(s => s.Name.ToLower() == name.ToLower() && !s.IsDeleted);
            
            if (excludeId.HasValue)
                query = query.Where(s => s.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> AssignSkillToUserAsync(int userId, int skillId, string assignedBy, AssignSkillToUserRequest? request = null)
        {
            // Check if assignment already exists
            var existingAssignment = await _context.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

            if (existingAssignment != null)
            {
                existingAssignment.IsActive = true;
                existingAssignment.AssignedAt = DateTime.UtcNow;
                existingAssignment.AssignedBy = assignedBy;
                
                if (request != null)
                {
                    existingAssignment.ProficiencyLevel = request.ProficiencyLevel;
                    existingAssignment.YearsOfExperience = request.YearsOfExperience;
                    existingAssignment.Certifications = request.Certifications != null ? JsonSerializer.Serialize(request.Certifications) : null;
                    existingAssignment.Notes = request.Notes;
                }
            }
            else
            {
                var userSkill = new UserSkill
                {
                    UserId = userId,
                    SkillId = skillId,
                    ProficiencyLevel = request?.ProficiencyLevel,
                    YearsOfExperience = request?.YearsOfExperience,
                    Certifications = request?.Certifications != null ? JsonSerializer.Serialize(request.Certifications) : null,
                    Notes = request?.Notes,
                    AssignedBy = assignedBy,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UserSkills.Add(userSkill);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveSkillFromUserAsync(int userId, int skillId)
        {
            var userSkill = await _context.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

            if (userSkill == null)
                return false;

            userSkill.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserSkillDto>> GetUserSkillsAsync(int userId)
        {
            var userSkills = await _context.UserSkills
                .Where(us => us.UserId == userId && us.IsActive)
                .Include(us => us.Skill)
                .Select(us => new
                {
                    us.Id,
                    us.UserId,
                    us.SkillId,
                    SkillName = us.Skill.Name,
                    SkillCategory = us.Skill.Category,
                    us.ProficiencyLevel,
                    us.YearsOfExperience,
                    us.Certifications,
                    us.Notes,
                    us.AssignedAt
                })
                .ToListAsync();

            return userSkills.Select(us => new UserSkillDto
            {
                Id = us.Id,
                UserId = us.UserId,
                SkillId = us.SkillId,
                SkillName = us.SkillName,
                SkillCategory = us.SkillCategory,
                ProficiencyLevel = us.ProficiencyLevel,
                YearsOfExperience = us.YearsOfExperience,
                Certifications = !string.IsNullOrEmpty(us.Certifications) ? JsonSerializer.Deserialize<string[]>(us.Certifications) : null,
                Notes = us.Notes,
                AssignedAt = us.AssignedAt
            });
        }

        public async Task<IEnumerable<SkillDto>> GetSkillsByCategoryAsync(string category)
        {
            return await _context.Skills
                .Where(s => !s.IsDeleted && s.IsActive && s.Category.ToLower() == category.ToLower())
                .Select(s => new SkillDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Category = s.Category,
                    Level = s.Level,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    IsActive = s.IsActive,
                    UserCount = s.UserSkills.Count(us => us.IsActive)
                })
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}