using MyApi.Data;
using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Models;
using MyApi.Modules.Contacts.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MyApi.Modules.Projects.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectService> _logger;
        private readonly IProjectColumnService _columnService;

        public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger, IProjectColumnService columnService)
        {
            _context = context;
            _logger = logger;
            _columnService = columnService;
        }

        public async Task<ProjectListResponseDto> GetAllProjectsAsync(ProjectSearchRequestDto? searchRequest = null)
        {
            try
            {
                var query = _context.Projects
                    .Include(p => p.Columns)
                    .Include(p => p.Contact)
                    .Where(p => !p.IsDeleted);

                // Apply filters
                if (searchRequest != null)
                {
                    if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
                    {
                        var searchTerm = searchRequest.SearchTerm.ToLower();
                        query = query.Where(p => p.Name.ToLower().Contains(searchTerm) ||
                                               (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
                    }

                    if (!string.IsNullOrEmpty(searchRequest.Status))
                        query = query.Where(p => p.Status == searchRequest.Status);

                    if (!string.IsNullOrEmpty(searchRequest.Type))
                        query = query.Where(p => p.Type == searchRequest.Type);

                    if (!string.IsNullOrEmpty(searchRequest.Priority))
                        query = query.Where(p => p.Priority == searchRequest.Priority);

                    if (searchRequest.OwnerId.HasValue)
                        query = query.Where(p => p.OwnerId == searchRequest.OwnerId.Value);

                    if (searchRequest.ContactId.HasValue)
                        query = query.Where(p => p.ContactId == searchRequest.ContactId.Value);

                    if (searchRequest.IsArchived.HasValue)
                        query = query.Where(p => p.IsArchived == searchRequest.IsArchived.Value);

                    // Team members filter
                    if (searchRequest.TeamMemberIds != null && searchRequest.TeamMemberIds.Any())
                    {
                        query = query.Where(p => searchRequest.TeamMemberIds.Any(memberId =>
                            p.TeamMembers != null && p.TeamMembers.Contains(memberId.ToString())));
                    }

                    // Date range filters
                    if (searchRequest.StartDateFrom.HasValue)
                        query = query.Where(p => p.StartDate >= searchRequest.StartDateFrom.Value);

                    if (searchRequest.StartDateTo.HasValue)
                        query = query.Where(p => p.StartDate <= searchRequest.StartDateTo.Value);

                    if (searchRequest.EndDateFrom.HasValue)
                        query = query.Where(p => p.EndDate >= searchRequest.EndDateFrom.Value);

                    if (searchRequest.EndDateTo.HasValue)
                        query = query.Where(p => p.EndDate <= searchRequest.EndDateTo.Value);

                    // Apply sorting
                    if (!string.IsNullOrEmpty(searchRequest.SortBy))
                    {
                        var isDescending = searchRequest.SortDirection?.ToLower() == "desc";
                        
                        query = searchRequest.SortBy.ToLower() switch
                        {
                            "name" => isDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                            "status" => isDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
                            "priority" => isDescending ? query.OrderByDescending(p => p.Priority) : query.OrderBy(p => p.Priority),
                            "startdate" => isDescending ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
                            "enddate" => isDescending ? query.OrderByDescending(p => p.EndDate) : query.OrderBy(p => p.EndDate),
                            "progress" => isDescending ? query.OrderByDescending(p => p.Progress) : query.OrderBy(p => p.Progress),
                            _ => query.OrderByDescending(p => p.CreatedAt)
                        };
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.CreatedAt);
                    }
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var pageNumber = searchRequest?.PageNumber ?? 1;
                var pageSize = searchRequest?.PageSize ?? 20;
                var skip = (pageNumber - 1) * pageSize;

                var projects = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var projectDtos = new List<ProjectResponseDto>();
                foreach (var project in projects)
                {
                    var dto = await MapToProjectDtoAsync(project);
                    projectDtos.Add(dto);
                }

                return new ProjectListResponseDto
                {
                    Projects = projectDtos,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    HasNextPage = skip + pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all projects");
                throw;
            }
        }

        public async Task<ProjectResponseDto?> GetProjectByIdAsync(int id)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.Columns)
                    .Include(p => p.Contact)
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                return project != null ? await MapToProjectDtoAsync(project) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project by id {ProjectId}", id);
                throw;
            }
        }

        public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectRequestDto createDto, string createdByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var project = new Project
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    ContactId = createDto.ContactId,
                    OwnerId = createDto.OwnerId,
                    OwnerName = createDto.OwnerName,
                    TeamMembers = JsonSerializer.Serialize(createDto.TeamMembers),
                    Budget = createDto.Budget,
                    Currency = createDto.Currency,
                    Status = createDto.Status,
                    Type = createDto.Type,
                    Priority = createDto.Priority,
                    StartDate = createDto.StartDate,
                    EndDate = createDto.EndDate,
                    Tags = JsonSerializer.Serialize(createDto.Tags),
                    CreatedBy = createdByUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Create default columns for the project
                await _columnService.CreateDefaultColumnsAsync(project.Id, createdByUser);

                await transaction.CommitAsync();

                // Reload project with related data
                var createdProject = await GetProjectByIdAsync(project.Id);
                _logger.LogInformation("Project created successfully with ID {ProjectId}", project.Id);
                
                return createdProject!;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating project");
                throw;
            }
        }

        public async Task<ProjectResponseDto?> UpdateProjectAsync(int id, UpdateProjectRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var project = await _context.Projects
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return null;

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Name))
                    project.Name = updateDto.Name;

                if (updateDto.Description != null)
                    project.Description = updateDto.Description;

                if (updateDto.ContactId.HasValue)
                    project.ContactId = updateDto.ContactId.Value;

                if (updateDto.OwnerId.HasValue)
                    project.OwnerId = updateDto.OwnerId.Value;

                if (!string.IsNullOrEmpty(updateDto.OwnerName))
                    project.OwnerName = updateDto.OwnerName;

                if (updateDto.TeamMembers != null)
                    project.TeamMembers = JsonSerializer.Serialize(updateDto.TeamMembers);

                if (updateDto.Budget.HasValue)
                    project.Budget = updateDto.Budget.Value;

                if (!string.IsNullOrEmpty(updateDto.Currency))
                    project.Currency = updateDto.Currency;

                if (!string.IsNullOrEmpty(updateDto.Status))
                    project.Status = updateDto.Status;

                if (!string.IsNullOrEmpty(updateDto.Type))
                    project.Type = updateDto.Type;

                if (!string.IsNullOrEmpty(updateDto.Priority))
                    project.Priority = updateDto.Priority;

                if (updateDto.Progress.HasValue)
                    project.Progress = Math.Max(0, Math.Min(100, updateDto.Progress.Value));

                if (updateDto.StartDate.HasValue)
                    project.StartDate = updateDto.StartDate.Value;

                if (updateDto.EndDate.HasValue)
                    project.EndDate = updateDto.EndDate.Value;

                if (updateDto.ActualStartDate.HasValue)
                    project.ActualStartDate = updateDto.ActualStartDate.Value;

                if (updateDto.ActualEndDate.HasValue)
                    project.ActualEndDate = updateDto.ActualEndDate.Value;

                if (updateDto.Tags != null)
                    project.Tags = JsonSerializer.Serialize(updateDto.Tags);

                if (updateDto.IsArchived.HasValue)
                    project.IsArchived = updateDto.IsArchived.Value;

                project.ModifiedBy = modifiedByUser;
                project.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reload project with related data
                var updatedProject = await GetProjectByIdAsync(id);
                _logger.LogInformation("Project updated successfully with ID {ProjectId}", id);
                
                return updatedProject;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project with ID {ProjectId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProjectAsync(int id, string deletedByUser)
        {
            try
            {
                var project = await _context.Projects
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return false;

                // Soft delete
                project.IsDeleted = true;
                project.ModifiedBy = deletedByUser;
                project.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Project soft deleted successfully with ID {ProjectId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project with ID {ProjectId}", id);
                throw;
            }
        }

        public async Task<ProjectListResponseDto> SearchProjectsAsync(string searchTerm, int pageNumber = 1, int pageSize = 20)
        {
            var searchRequest = new ProjectSearchRequestDto
            {
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAllProjectsAsync(searchRequest);
        }

        public async Task<ProjectListResponseDto> GetProjectsByOwnerAsync(int ownerId, int pageNumber = 1, int pageSize = 20)
        {
            var searchRequest = new ProjectSearchRequestDto
            {
                OwnerId = ownerId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAllProjectsAsync(searchRequest);
        }

        public async Task<ProjectListResponseDto> GetProjectsByContactAsync(int contactId, int pageNumber = 1, int pageSize = 20)
        {
            var searchRequest = new ProjectSearchRequestDto
            {
                ContactId = contactId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAllProjectsAsync(searchRequest);
        }

        public async Task<ProjectListResponseDto> GetProjectsByTeamMemberAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            var searchRequest = new ProjectSearchRequestDto
            {
                TeamMemberIds = new List<int> { userId },
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAllProjectsAsync(searchRequest);
        }

        public async Task<bool> AssignTeamMemberAsync(int projectId, AssignTeamMemberDto assignDto, string assignedByUser)
        {
            try
            {
                var project = await _context.Projects
                    .Where(p => p.Id == projectId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return false;

                var teamMembers = string.IsNullOrEmpty(project.TeamMembers) 
                    ? new List<int>() 
                    : JsonSerializer.Deserialize<List<int>>(project.TeamMembers) ?? new List<int>();

                if (!teamMembers.Contains(assignDto.UserId))
                {
                    teamMembers.Add(assignDto.UserId);
                    project.TeamMembers = JsonSerializer.Serialize(teamMembers);
                    project.ModifiedBy = assignedByUser;
                    project.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Team member {UserId} assigned to project {ProjectId}", assignDto.UserId, projectId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning team member {UserId} to project {ProjectId}", assignDto.UserId, projectId);
                throw;
            }
        }

        public async Task<bool> RemoveTeamMemberAsync(int projectId, RemoveTeamMemberDto removeDto, string removedByUser)
        {
            try
            {
                var project = await _context.Projects
                    .Where(p => p.Id == projectId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return false;

                var teamMembers = string.IsNullOrEmpty(project.TeamMembers) 
                    ? new List<int>() 
                    : JsonSerializer.Deserialize<List<int>>(project.TeamMembers) ?? new List<int>();

                if (teamMembers.Contains(removeDto.UserId))
                {
                    teamMembers.Remove(removeDto.UserId);
                    project.TeamMembers = JsonSerializer.Serialize(teamMembers);
                    project.ModifiedBy = removedByUser;
                    project.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Team member {UserId} removed from project {ProjectId}", removeDto.UserId, projectId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing team member {UserId} from project {ProjectId}", removeDto.UserId, projectId);
                throw;
            }
        }

        public async Task<List<int>> GetProjectTeamMembersAsync(int projectId)
        {
            try
            {
                var project = await _context.Projects
                    .Where(p => p.Id == projectId && !p.IsDeleted)
                    .Select(p => p.TeamMembers)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(project))
                    return new List<int>();

                return JsonSerializer.Deserialize<List<int>>(project) ?? new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<ProjectStatsDto> GetProjectStatsAsync(int projectId)
        {
            try
            {
                var totalTasks = await _context.ProjectTasks
                    .CountAsync(t => t.ProjectId == projectId && !t.IsDeleted);

                var completedTasks = await _context.ProjectTasks
                    .CountAsync(t => t.ProjectId == projectId && !t.IsDeleted && t.CompletedAt.HasValue);

                var overdueTasks = await _context.ProjectTasks
                    .CountAsync(t => t.ProjectId == projectId && !t.IsDeleted && 
                               t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && !t.CompletedAt.HasValue);

                var teamMembers = await GetProjectTeamMembersAsync(projectId);
                var activeMembers = teamMembers.Count;

                var completionPercentage = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0;

                return new ProjectStatsDto
                {
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    OverdueTasks = overdueTasks,
                    ActiveMembers = activeMembers,
                    CompletionPercentage = Math.Round(completionPercentage, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<Dictionary<int, ProjectStatsDto>> GetMultipleProjectStatsAsync(List<int> projectIds)
        {
            var stats = new Dictionary<int, ProjectStatsDto>();

            foreach (var projectId in projectIds)
            {
                stats[projectId] = await GetProjectStatsAsync(projectId);
            }

            return stats;
        }

        public async Task<bool> BulkUpdateProjectStatusAsync(BulkUpdateProjectStatusDto bulkUpdateDto, string updatedByUser)
        {
            try
            {
                var projects = await _context.Projects
                    .Where(p => bulkUpdateDto.ProjectIds.Contains(p.Id) && !p.IsDeleted)
                    .ToListAsync();

                foreach (var project in projects)
                {
                    project.Status = bulkUpdateDto.Status;
                    project.ModifiedBy = updatedByUser;
                    project.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk updated status for {Count} projects", projects.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk project status update");
                throw;
            }
        }

        public async Task<bool> BulkArchiveProjectsAsync(List<int> projectIds, bool archive, string updatedByUser)
        {
            try
            {
                var projects = await _context.Projects
                    .Where(p => projectIds.Contains(p.Id) && !p.IsDeleted)
                    .ToListAsync();

                foreach (var project in projects)
                {
                    project.IsArchived = archive;
                    project.ModifiedBy = updatedByUser;
                    project.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk {Action} {Count} projects", archive ? "archived" : "unarchived", projects.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk project archive operation");
                throw;
            }
        }

        public async Task<bool> ProjectExistsAsync(int id)
        {
            return await _context.Projects.AnyAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<bool> UserCanAccessProjectAsync(int projectId, int userId)
        {
            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (project == null)
                return false;

            // Owner can always access
            if (project.OwnerId == userId)
                return true;

            // Check if user is team member
            return await UserIsProjectTeamMemberAsync(projectId, userId);
        }

        public async Task<bool> UserIsProjectOwnerAsync(int projectId, int userId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.OwnerId == userId && !p.IsDeleted);
        }

        public async Task<bool> UserIsProjectTeamMemberAsync(int projectId, int userId)
        {
            var teamMembers = await GetProjectTeamMembersAsync(projectId);
            return teamMembers.Contains(userId);
        }

        private async Task<ProjectResponseDto> MapToProjectDtoAsync(Project project)
        {
            var teamMembers = string.IsNullOrEmpty(project.TeamMembers) 
                ? new List<int>() 
                : JsonSerializer.Deserialize<List<int>>(project.TeamMembers) ?? new List<int>();

            var tags = string.IsNullOrEmpty(project.Tags) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(project.Tags) ?? new List<string>();

            var columns = project.Columns?.Select(c => new ProjectColumnResponseDto
            {
                Id = c.Id,
                ProjectId = c.ProjectId,
                Title = c.Title,
                Color = c.Color,
                Position = c.Position,
                IsDefault = c.IsDefault,
                TaskLimit = c.TaskLimit,
                CreatedAt = c.CreatedAt,
                TaskCount = _context.ProjectTasks.Count(t => t.ColumnId == c.Id && !t.IsDeleted)
            }).OrderBy(c => c.Position).ToList() ?? new List<ProjectColumnResponseDto>();

            var stats = await GetProjectStatsAsync(project.Id);

            return new ProjectResponseDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                ContactId = project.ContactId,
                ContactName = project.Contact?.Name,
                OwnerId = project.OwnerId,
                OwnerName = project.OwnerName,
                TeamMembers = teamMembers,
                TeamMemberNames = new List<string>(), // TODO: Load actual names if needed
                Budget = project.Budget,
                Currency = project.Currency,
                Status = project.Status,
                Type = project.Type,
                Priority = project.Priority,
                Progress = project.Progress,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                ActualStartDate = project.ActualStartDate,
                ActualEndDate = project.ActualEndDate,
                Tags = tags,
                IsArchived = project.IsArchived,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                CreatedBy = project.CreatedBy,
                ModifiedBy = project.ModifiedBy,
                Columns = columns,
                Stats = stats
            };
        }
    }
}
