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
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                var columnDtos = new List<ProjectColumnResponseDto>();
                foreach (var column in columns)
                {
                    var taskCount = await _context.ProjectTasks
                        .CountAsync(t => t.ColumnId == column.Id);

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
                    .CountAsync(t => t.ColumnId == id);

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
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == createDto.ProjectId);
                if (!projectExists)
                    throw new InvalidOperationException("Project not found");

                // Get next display order if not specified
                var displayOrder = createDto.DisplayOrder;
                if (displayOrder <= 0)
                {
                    displayOrder = await GetNextColumnDisplayOrderAsync(createDto.ProjectId);
                }

                var column = new ProjectColumn
                {
                    ProjectId = createDto.ProjectId,
                    Name = createDto.Name,
                    Color = createDto.Color,
                    DisplayOrder = displayOrder
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
                if (!string.IsNullOrEmpty(updateDto.Name))
                    column.Name = updateDto.Name;

                if (!string.IsNullOrEmpty(updateDto.Color))
                    column.Color = updateDto.Color;

                if (updateDto.DisplayOrder.HasValue)
                    column.DisplayOrder = updateDto.DisplayOrder.Value;

                await _context.SaveChangesAsync();

                var taskCount = await _context.ProjectTasks
                    .CountAsync(t => t.ColumnId == id);

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
                    .Where(t => t.ColumnId == id)
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
                        var nextDisplayOrder = await GetNextTaskDisplayOrderInColumnAsync(moveTasksToColumnId.Value);
                        foreach (var task in tasksInColumn)
                        {
                            task.ColumnId = moveTasksToColumnId.Value;
                            task.DisplayOrder = nextDisplayOrder++;
                            task.ModifiedBy = deletedByUser;
                            task.ModifiedDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Delete all tasks in the column
                        _context.ProjectTasks.RemoveRange(tasksInColumn);
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
                        column.DisplayOrder = columnPositionDto.DisplayOrder;
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

        public async Task<int> GetNextColumnDisplayOrderAsync(int projectId)
        {
            var maxDisplayOrder = await _context.ProjectColumns
                .Where(c => c.ProjectId == projectId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

            return maxDisplayOrder + 1;
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
            // Check if project exists
            var project = await _context.Projects
                .Where(p => p.Id == projectId)
                .FirstOrDefaultAsync();

            if (project == null)
                return false;

            // For now, allow all authenticated users to manage columns
            return true;
        }

        public async Task<bool> CanDeleteColumnAsync(int columnId)
        {
            // Check if column has tasks
            var hasActiveTasks = await _context.ProjectTasks
                .AnyAsync(t => t.ColumnId == columnId);

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
                .CountAsync(t => t.ColumnId == columnId);
        }

        public async Task<bool> CreateDefaultColumnsAsync(int projectId, string createdByUser)
        {
            try
            {
                var defaultColumns = new[]
                {
                    new ProjectColumn { ProjectId = projectId, Name = "To Do", Color = "#64748b", DisplayOrder = 1 },
                    new ProjectColumn { ProjectId = projectId, Name = "In Progress", Color = "#3b82f6", DisplayOrder = 2 },
                    new ProjectColumn { ProjectId = projectId, Name = "Review", Color = "#f59e0b", DisplayOrder = 3 },
                    new ProjectColumn { ProjectId = projectId, Name = "Done", Color = "#10b981", DisplayOrder = 4 }
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

        public Task<List<ProjectColumnResponseDto>> GetDefaultColumnTemplatesAsync()
        {
            return Task.FromResult(new List<ProjectColumnResponseDto>
            {
                new() { Name = "To Do", Color = "#64748b", DisplayOrder = 1 },
                new() { Name = "In Progress", Color = "#3b82f6", DisplayOrder = 2 },
                new() { Name = "Review", Color = "#f59e0b", DisplayOrder = 3 },
                new() { Name = "Done", Color = "#10b981", DisplayOrder = 4 }
            });
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

        private async Task<int> GetNextTaskDisplayOrderInColumnAsync(int columnId)
        {
            var maxDisplayOrder = await _context.ProjectTasks
                .Where(t => t.ColumnId == columnId)
                .MaxAsync(t => (int?)t.DisplayOrder) ?? 0;

            return maxDisplayOrder + 1;
        }

        private static ProjectColumnResponseDto MapToColumnDto(ProjectColumn column, int taskCount)
        {
            return new ProjectColumnResponseDto
            {
                Id = column.Id,
                ProjectId = column.ProjectId,
                Name = column.Name,
                Color = column.Color,
                DisplayOrder = column.DisplayOrder,
                TaskCount = taskCount
            };
        }
    }
}
