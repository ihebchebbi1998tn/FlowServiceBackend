using MyApi.Data;
using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Projects.Services
{
    public class ProjectColumnService : IProjectColumnService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectColumnService> _logger;

        public ProjectColumnService(ApplicationDbContext context, ILogger<ProjectColumnService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ProjectColumnListResponseDto> GetProjectColumnsAsync(int projectId)
        {
            try
            {
                var columns = await _context.ProjectColumns
                    .Where(c => c.ProjectId == projectId)
                    .OrderBy(c => c.Position)
                    .ToListAsync();

                var columnDtos = new List<ProjectColumnResponseDto>();
                foreach (var column in columns)
                {
                    var taskCount = await _context.ProjectTasks
                        .CountAsync(t => t.ColumnId == column.Id && !t.IsDeleted);

                    columnDtos.Add(MapToColumnDto(column, taskCount));
                }

                return new ProjectColumnListResponseDto
                {
                    Columns = columnDtos,
                    TotalCount = columnDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting columns for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<ProjectColumnResponseDto?> GetColumnByIdAsync(int id)
        {
            try
            {
                var column = await _context.ProjectColumns
                    .Where(c => c.Id == id)
                    .FirstOrDefaultAsync();

                if (column == null)
                    return null;

                var taskCount = await _context.ProjectTasks
                    .CountAsync(t => t.ColumnId == id && !t.IsDeleted);

                return MapToColumnDto(column, taskCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting column by id {ColumnId}", id);
                throw;
            }
        }

        public async Task<ProjectColumnResponseDto> CreateColumnAsync(CreateProjectColumnRequestDto createDto, string createdByUser)
        {
            try
            {
                // Validate project exists
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == createDto.ProjectId && !p.IsDeleted);
                if (!projectExists)
                    throw new InvalidOperationException("Project not found");

                // Get next position if not specified
                var position = createDto.Position;
                if (position <= 0)
                {
                    position = await GetNextColumnPositionAsync(createDto.ProjectId);
                }

                var column = new ProjectColumn
                {
                    ProjectId = createDto.ProjectId,
                    Title = createDto.Title,
                    Color = createDto.Color,
                    Position = position,
                    IsDefault = createDto.IsDefault,
                    TaskLimit = createDto.TaskLimit,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProjectColumns.Add(column);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Column created successfully with ID {ColumnId}", column.Id);
                return MapToColumnDto(column, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating column");
                throw;
            }
        }

        public async Task<ProjectColumnResponseDto?> UpdateColumnAsync(int id, UpdateProjectColumnRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var column = await _context.ProjectColumns
                    .Where(c => c.Id == id)
                    .FirstOrDefaultAsync();

                if (column == null)
                    return null;

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Title))
                    column.Title = updateDto.Title;

                if (!string.IsNullOrEmpty(updateDto.Color))
                    column.Color = updateDto.Color;

                if (updateDto.Position.HasValue)
                    column.Position = updateDto.Position.Value;

                if (updateDto.IsDefault.HasValue)
                    column.IsDefault = updateDto.IsDefault.Value;

                if (updateDto.TaskLimit.HasValue)
                    column.TaskLimit = updateDto.TaskLimit.Value;

                await _context.SaveChangesAsync();

                var taskCount = await _context.ProjectTasks
                    .CountAsync(t => t.ColumnId == id && !t.IsDeleted);

                _logger.LogInformation("Column updated successfully with ID {ColumnId}", id);
                return MapToColumnDto(column, taskCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating column with ID {ColumnId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteColumnAsync(int id, int? moveTasksToColumnId, string deletedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var column = await _context.ProjectColumns
                    .Where(c => c.Id == id)
                    .FirstOrDefaultAsync();

                if (column == null)
                    return false;

                // Handle tasks in the column
                var tasksInColumn = await _context.ProjectTasks
                    .Where(t => t.ColumnId == id && !t.IsDeleted)
                    .ToListAsync();

                if (tasksInColumn.Any())
                {
                    if (moveTasksToColumnId.HasValue)
                    {
                        // Verify target column exists and belongs to same project
                        var targetColumn = await _context.ProjectColumns
                            .Where(c => c.Id == moveTasksToColumnId.Value && c.ProjectId == column.ProjectId)
                            .FirstOrDefaultAsync();

                        if (targetColumn == null)
                            throw new InvalidOperationException("Target column not found or doesn't belong to the same project");

                        // Move tasks to target column
                        var nextPosition = await GetNextTaskPositionInColumnAsync(moveTasksToColumnId.Value);
                        foreach (var task in tasksInColumn)
                        {
                            task.ColumnId = moveTasksToColumnId.Value;
                            task.Position = nextPosition++;
                            task.ModifiedBy = deletedByUser;
                            task.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Soft delete all tasks in the column
                        foreach (var task in tasksInColumn)
                        {
                            task.IsDeleted = true;
                            task.ModifiedBy = deletedByUser;
                            task.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // Delete the column
                _context.ProjectColumns.Remove(column);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Column deleted successfully with ID {ColumnId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting column with ID {ColumnId}", id);
                throw;
            }
        }

        public async Task<bool> ReorderColumnsAsync(int projectId, ReorderProjectColumnsRequestDto reorderDto, string updatedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var columns = await _context.ProjectColumns
                    .Where(c => c.ProjectId == projectId)
                    .ToListAsync();

                foreach (var columnPositionDto in reorderDto.Columns)
                {
                    var column = columns.FirstOrDefault(c => c.Id == columnPositionDto.Id);
                    if (column != null)
                    {
                        column.Position = columnPositionDto.Position;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Columns reordered successfully for project {ProjectId}", projectId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error reordering columns for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<int> GetNextColumnPositionAsync(int projectId)
        {
            var maxPosition = await _context.ProjectColumns
                .Where(c => c.ProjectId == projectId)
                .MaxAsync(c => (int?)c.Position) ?? 0;

            return maxPosition + 1;
        }

        public async Task<bool> ColumnExistsAsync(int id)
        {
            return await _context.ProjectColumns.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> ColumnBelongsToProjectAsync(int columnId, int projectId)
        {
            return await _context.ProjectColumns
                .AnyAsync(c => c.Id == columnId && c.ProjectId == projectId);
        }

        public async Task<bool> UserCanManageProjectColumnsAsync(int projectId, int userId)
        {
            // Check if user is project owner or team member
            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (project == null)
                return false;

            // Owner can always manage
            if (project.OwnerId == userId)
                return true;

            // Check if user is team member (basic implementation)
            // In a real scenario, you might want to check specific permissions
            return !string.IsNullOrEmpty(project.TeamMembers) && 
                   project.TeamMembers.Contains(userId.ToString());
        }

        public async Task<bool> CanDeleteColumnAsync(int columnId)
        {
            // Check if column has tasks
            var hasActiveTasks = await _context.ProjectTasks
                .AnyAsync(t => t.ColumnId == columnId && !t.IsDeleted);

            // Check if it's the only column in the project
            var column = await _context.ProjectColumns.FindAsync(columnId);
            if (column == null)
                return false;

            var columnCount = await _context.ProjectColumns
                .CountAsync(c => c.ProjectId == column.ProjectId);

            // Don't allow deletion if it's the only column
            return columnCount > 1;
        }

        public async Task<int> GetColumnTaskCountAsync(int columnId)
        {
            return await _context.ProjectTasks
                .CountAsync(t => t.ColumnId == columnId && !t.IsDeleted);
        }

        public async Task<bool> CreateDefaultColumnsAsync(int projectId, string createdByUser)
        {
            try
            {
                var defaultColumns = new[]
                {
                    new ProjectColumn { ProjectId = projectId, Title = "To Do", Color = "#64748b", Position = 1, IsDefault = true, CreatedAt = DateTime.UtcNow },
                    new ProjectColumn { ProjectId = projectId, Title = "In Progress", Color = "#3b82f6", Position = 2, IsDefault = true, CreatedAt = DateTime.UtcNow },
                    new ProjectColumn { ProjectId = projectId, Title = "Review", Color = "#f59e0b", Position = 3, IsDefault = true, CreatedAt = DateTime.UtcNow },
                    new ProjectColumn { ProjectId = projectId, Title = "Done", Color = "#10b981", Position = 4, IsDefault = true, CreatedAt = DateTime.UtcNow }
                };

                _context.ProjectColumns.AddRange(defaultColumns);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Default columns created for project {ProjectId}", projectId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default columns for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<ProjectColumnResponseDto>> GetDefaultColumnTemplatesAsync()
        {
            return new List<ProjectColumnResponseDto>
            {
                new() { Title = "To Do", Color = "#64748b", Position = 1, IsDefault = true },
                new() { Title = "In Progress", Color = "#3b82f6", Position = 2, IsDefault = true },
                new() { Title = "Review", Color = "#f59e0b", Position = 3, IsDefault = true },
                new() { Title = "Done", Color = "#10b981", Position = 4, IsDefault = true }
            };
        }

        public async Task<bool> BulkDeleteColumnsAsync(BulkDeleteProjectColumnsDto bulkDeleteDto, string deletedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var columnId in bulkDeleteDto.ColumnIds)
                {
                    await DeleteColumnAsync(columnId, bulkDeleteDto.MoveTasksToColumnId, deletedByUser);
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Bulk deleted {Count} columns", bulkDeleteDto.ColumnIds.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during bulk column deletion");
                throw;
            }
        }

        public async Task<bool> BulkUpdateColumnColorsAsync(Dictionary<int, string> columnColors, string updatedByUser)
        {
            try
            {
                var columns = await _context.ProjectColumns
                    .Where(c => columnColors.Keys.Contains(c.Id))
                    .ToListAsync();

                foreach (var column in columns)
                {
                    if (columnColors.TryGetValue(column.Id, out var color))
                    {
                        column.Color = color;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk updated colors for {Count} columns", columns.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk column color update");
                throw;
            }
        }

        private async Task<int> GetNextTaskPositionInColumnAsync(int columnId)
        {
            var maxPosition = await _context.ProjectTasks
                .Where(t => t.ColumnId == columnId && !t.IsDeleted)
                .MaxAsync(t => (int?)t.Position) ?? 0;

            return maxPosition + 1;
        }

        private static ProjectColumnResponseDto MapToColumnDto(ProjectColumn column, int taskCount)
        {
            return new ProjectColumnResponseDto
            {
                Id = column.Id,
                ProjectId = column.ProjectId,
                Title = column.Title,
                Color = column.Color,
                Position = column.Position,
                IsDefault = column.IsDefault,
                TaskLimit = column.TaskLimit,
                CreatedAt = column.CreatedAt,
                TaskCount = taskCount
            };
        }
    }
}
