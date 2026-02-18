using MyApi.Data;
using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Models;
using MyApi.Modules.Notifications.Services;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Modules.Projects.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;
        private readonly INotificationService _notificationService;

        public TaskService(ApplicationDbContext context, ILogger<TaskService> logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        #region Project Tasks

        public async Task<List<ProjectTaskResponseDto>> GetProjectTasksAsync(int projectId)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .AsNoTracking()
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.AssignedUser)
                    .Where(t => t.ProjectId == projectId)
                    .OrderBy(t => t.Column.DisplayOrder)
                    .ThenBy(t => t.DisplayOrder)
                    .ToListAsync();

                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<ProjectTaskResponseDto>> GetColumnTasksAsync(int columnId)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .AsNoTracking()
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.AssignedUser)
                    .Where(t => t.ColumnId == columnId)
                    .OrderBy(t => t.DisplayOrder)
                    .ToListAsync();

                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for column {ColumnId}", columnId);
                throw;
            }
        }

        public async Task<ProjectTaskResponseDto?> GetProjectTaskByIdAsync(int id)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .AsNoTracking()
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.AssignedUser)
                    .Where(t => t.Id == id)
                    .FirstOrDefaultAsync();

                return task != null ? MapToProjectTaskDto(task) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project task by id {TaskId}", id);
                throw;
            }
        }

        public async Task<ProjectTaskResponseDto> CreateProjectTaskAsync(CreateProjectTaskRequestDto createDto, string createdByUser)
        {
            try
            {
                // Validate project and column exist
                var column = await _context.ProjectColumns
                    .Where(c => c.Id == createDto.ColumnId && c.ProjectId == createDto.ProjectId)
                    .FirstOrDefaultAsync();

                if (column == null)
                    throw new InvalidOperationException("Column not found or doesn't belong to the project");

                // Get next display order in column
                var displayOrder = createDto.DisplayOrder ?? await GetNextTaskDisplayOrderAsync(createDto.ColumnId);

                var task = new ProjectTask
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    ProjectId = createDto.ProjectId,
                    ColumnId = createDto.ColumnId,
                    Priority = createDto.Priority,
                    DueDate = createDto.DueDate,
                    AssignedUserId = createDto.AssignedUserId,
                    DisplayOrder = displayOrder,
                    CreatedBy = createdByUser,
                    CreatedDate = DateTime.UtcNow
                };

                _context.ProjectTasks.Add(task);
                await _context.SaveChangesAsync();

                // Reload task with related data
                var createdTask = await GetProjectTaskByIdAsync(task.Id);
                _logger.LogInformation("Project task created successfully with ID {TaskId}", task.Id);
                
                return createdTask!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project task");
                throw;
            }
        }

        public async Task<ProjectTaskResponseDto?> UpdateProjectTaskAsync(int id, UpdateProjectTaskRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Where(t => t.Id == id)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return null;

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Title))
                    task.Title = updateDto.Title;

                if (updateDto.Description != null)
                    task.Description = updateDto.Description;

                if (updateDto.ColumnId.HasValue)
                    task.ColumnId = updateDto.ColumnId.Value;

                if (!string.IsNullOrEmpty(updateDto.Priority))
                    task.Priority = updateDto.Priority;

                if (updateDto.DueDate.HasValue)
                    task.DueDate = updateDto.DueDate.Value;

                if (updateDto.AssignedUserId.HasValue)
                    task.AssignedUserId = updateDto.AssignedUserId.Value;

                if (updateDto.DisplayOrder.HasValue)
                    task.DisplayOrder = updateDto.DisplayOrder.Value;

                task.ModifiedBy = modifiedByUser;
                task.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reload task with related data
                var updatedTask = await GetProjectTaskByIdAsync(id);
                _logger.LogInformation("Project task updated successfully with ID {TaskId}", id);
                
                return updatedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project task with ID {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProjectTaskAsync(int id, string deletedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Where(t => t.Id == id)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                _context.ProjectTasks.Remove(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Project task deleted successfully with ID {TaskId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project task with ID {TaskId}", id);
                throw;
            }
        }

        #endregion

        #region Daily Tasks

        public async Task<List<DailyTaskResponseDto>> GetUserDailyTasksAsync(int userId)
        {
            try
            {
                var tasks = await _context.DailyTasks
                    .Include(t => t.AssignedUser)
                    .Where(t => t.AssignedUserId == userId)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();

                return tasks.Select(MapToDailyTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily tasks for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<DailyTaskResponseDto>> GetDailyTasksByDateAsync(DateTime date)
        {
            try
            {
                var tasks = await _context.DailyTasks
                    .Include(t => t.AssignedUser)
                    .Where(t => t.DueDate.Date == date.Date)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();

                return tasks.Select(MapToDailyTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily tasks for date {Date}", date);
                throw;
            }
        }

        public async Task<DailyTaskResponseDto?> GetDailyTaskByIdAsync(int id)
        {
            try
            {
                var task = await _context.DailyTasks
                    .Include(t => t.AssignedUser)
                    .Where(t => t.Id == id)
                    .FirstOrDefaultAsync();

                return task != null ? MapToDailyTaskDto(task) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily task by id {TaskId}", id);
                throw;
            }
        }

        public async Task<DailyTaskResponseDto> CreateDailyTaskAsync(CreateDailyTaskRequestDto createDto, string createdByUser)
        {
            try
            {
                var task = new DailyTask
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    DueDate = createDto.DueDate,
                    AssignedUserId = createDto.AssignedUserId,
                    Priority = createDto.Priority,
                    Status = createDto.Status ?? "todo",
                    IsCompleted = false,
                    CreatedBy = createdByUser,
                    CreatedDate = DateTime.UtcNow
                };

                _context.DailyTasks.Add(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Daily task created successfully with ID {TaskId}", task.Id);
                return MapToDailyTaskDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating daily task");
                throw;
            }
        }

        public async Task<DailyTaskResponseDto?> UpdateDailyTaskAsync(int id, UpdateDailyTaskRequestDto updateDto, string modifiedByUser)
        {
            try
            {
                var task = await _context.DailyTasks
                    .Where(t => t.Id == id)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return null;

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Title))
                    task.Title = updateDto.Title;

                if (updateDto.Description != null)
                    task.Description = updateDto.Description;

                if (updateDto.DueDate.HasValue)
                    task.DueDate = updateDto.DueDate.Value;

                if (updateDto.IsCompleted.HasValue)
                {
                    task.IsCompleted = updateDto.IsCompleted.Value;
                    // Auto-update status based on completion
                    if (updateDto.IsCompleted.Value)
                    {
                        task.Status = "done";
                        task.CompletedDate = DateTime.UtcNow;
                    }
                }

                if (updateDto.CompletedDate.HasValue)
                    task.CompletedDate = updateDto.CompletedDate.Value;

                if (updateDto.AssignedUserId.HasValue)
                    task.AssignedUserId = updateDto.AssignedUserId.Value;

                if (!string.IsNullOrEmpty(updateDto.Priority))
                    task.Priority = updateDto.Priority;

                // Handle status update
                if (!string.IsNullOrEmpty(updateDto.Status))
                {
                    task.Status = updateDto.Status;
                    // Sync IsCompleted with status
                    task.IsCompleted = updateDto.Status == "done";
                    if (updateDto.Status == "done" && !task.CompletedDate.HasValue)
                    {
                        task.CompletedDate = DateTime.UtcNow;
                    }
                    else if (updateDto.Status != "done")
                    {
                        task.CompletedDate = null;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Daily task updated successfully with ID {TaskId}", id);
                return MapToDailyTaskDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating daily task with ID {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDailyTaskAsync(int id, string deletedByUser)
        {
            try
            {
                var task = await _context.DailyTasks
                    .Where(t => t.Id == id)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                _context.DailyTasks.Remove(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Daily task deleted successfully with ID {TaskId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting daily task with ID {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> CompleteDailyTaskAsync(int id, string completedByUser)
        {
            try
            {
                var task = await _context.DailyTasks
                    .Where(t => t.Id == id)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                task.IsCompleted = true;
                task.CompletedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Daily task {TaskId} marked as completed", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing daily task {TaskId}", id);
                throw;
            }
        }

        #endregion

        #region Task Search and Filtering

        public async Task<TaskListResponseDto> SearchTasksAsync(TaskSearchRequestDto searchRequest)
        {
            try
            {
                var projectTasksQuery = _context.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.AssignedUser)
                    .AsQueryable();

                // Apply filters for project tasks
                if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
                {
                    var searchTerm = searchRequest.SearchTerm.ToLower();
                    projectTasksQuery = projectTasksQuery.Where(t => 
                        t.Title.ToLower().Contains(searchTerm) ||
                        (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(searchRequest.Priority))
                    projectTasksQuery = projectTasksQuery.Where(t => t.Priority == searchRequest.Priority);

                if (searchRequest.ProjectId.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.ProjectId == searchRequest.ProjectId.Value);

                if (searchRequest.ColumnId.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.ColumnId == searchRequest.ColumnId.Value);

                if (searchRequest.AssignedUserId.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.AssignedUserId == searchRequest.AssignedUserId.Value);

                if (searchRequest.DueDateFrom.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.DueDate >= searchRequest.DueDateFrom.Value);

                if (searchRequest.DueDateTo.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.DueDate <= searchRequest.DueDateTo.Value);

                // Apply pagination
                var totalCount = await projectTasksQuery.CountAsync();
                var skip = (searchRequest.PageNumber - 1) * searchRequest.PageSize;

                var projectTasks = await projectTasksQuery
                    .OrderByDescending(t => t.CreatedDate)
                    .Skip(skip)
                    .Take(searchRequest.PageSize)
                    .ToListAsync();

                var projectTaskDtos = projectTasks.Select(MapToProjectTaskDto).ToList();

                return new TaskListResponseDto
                {
                    ProjectTasks = projectTaskDtos,
                    DailyTasks = new List<DailyTaskResponseDto>(),
                    TotalCount = totalCount,
                    PageSize = searchRequest.PageSize,
                    PageNumber = searchRequest.PageNumber,
                    HasNextPage = skip + searchRequest.PageSize < totalCount,
                    HasPreviousPage = searchRequest.PageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching tasks");
                throw;
            }
        }

        public async Task<List<ProjectTaskResponseDto>> GetTasksByAssigneeAsync(int assigneeId, int? projectId = null)
        {
            try
            {
                var query = _context.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.AssignedUser)
                    .Where(t => t.AssignedUserId == assigneeId);

                if (projectId.HasValue)
                    query = query.Where(t => t.ProjectId == projectId.Value);

                var tasks = await query.ToListAsync();
                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<List<ProjectTaskResponseDto>> GetOverdueTasksAsync(int? projectId = null, int? assigneeId = null)
        {
            try
            {
                var query = _context.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.AssignedUser)
                    .Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow);

                if (projectId.HasValue)
                    query = query.Where(t => t.ProjectId == projectId.Value);

                if (assigneeId.HasValue)
                    query = query.Where(t => t.AssignedUserId == assigneeId.Value);

                var tasks = await query.ToListAsync();
                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks");
                throw;
            }
        }

        #endregion

        #region Task Movement and Positioning

        public async Task<bool> MoveTaskAsync(int taskId, MoveTaskRequestDto moveDto, string movedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Where(t => t.Id == taskId)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                // Validate column exists and belongs to same project
                var column = await _context.ProjectColumns
                    .Where(c => c.Id == moveDto.ColumnId && c.ProjectId == task.ProjectId)
                    .FirstOrDefaultAsync();

                if (column == null)
                    throw new InvalidOperationException("Target column not found or doesn't belong to the same project");

                task.ColumnId = moveDto.ColumnId;
                task.DisplayOrder = moveDto.DisplayOrder;
                task.ModifiedBy = movedByUser;
                task.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} moved successfully", taskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> BulkMoveTasksAsync(BulkMoveTasksRequestDto bulkMoveDto, string movedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var taskMove in bulkMoveDto.Tasks)
                {
                    var moveDto = new MoveTaskRequestDto
                    {
                        ColumnId = taskMove.ColumnId,
                        DisplayOrder = taskMove.DisplayOrder
                    };

                    await MoveTaskAsync(taskMove.Id, moveDto, movedByUser);
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Bulk moved {Count} tasks", bulkMoveDto.Tasks.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during bulk task move");
                throw;
            }
        }

        public async Task<int> GetNextTaskDisplayOrderAsync(int columnId)
        {
            var maxDisplayOrder = await _context.ProjectTasks
                .Where(t => t.ColumnId == columnId)
                .MaxAsync(t => (int?)t.DisplayOrder) ?? 0;

            return maxDisplayOrder + 1;
        }

        public async Task<bool> ReorderTasksInColumnAsync(int columnId, List<int> taskIds, string updatedByUser)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Where(t => taskIds.Contains(t.Id) && t.ColumnId == columnId)
                    .ToListAsync();

                for (int i = 0; i < taskIds.Count; i++)
                {
                    var task = tasks.FirstOrDefault(t => t.Id == taskIds[i]);
                    if (task != null)
                    {
                        task.DisplayOrder = i + 1;
                        task.ModifiedBy = updatedByUser;
                        task.ModifiedDate = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Reordered {Count} tasks in column {ColumnId}", tasks.Count, columnId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering tasks in column {ColumnId}", columnId);
                throw;
            }
        }

        #endregion

        #region Task Assignment

        public async Task<bool> AssignTaskAsync(int taskId, AssignTaskRequestDto assignDto, string assignedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                    .Where(t => t.Id == taskId)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                var previousAssigneeId = task.AssignedUserId;
                task.AssignedUserId = assignDto.AssignedUserId;
                task.ModifiedBy = assignedByUser;
                task.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification if assigning to a different user (not self-assignment)
                if (assignDto.AssignedUserId.HasValue &&
                    assignDto.AssignedUserId.Value > 0 &&
                    assignDto.AssignedUserId != previousAssigneeId)
                {
                    try
                    {
                        await _notificationService.GenerateTaskAssignedNotificationAsync(
                            taskId,
                            task.Title,
                            assignDto.AssignedUserId.Value,
                            assignedByUser,
                            task.ProjectId
                        );
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Failed to send task assignment notification for task {TaskId}", taskId);
                        // Don't fail the assignment if notification fails
                    }
                }

                _logger.LogInformation("Task {TaskId} assigned to user {AssignedUserId}", taskId, assignDto.AssignedUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> UnassignTaskAsync(int taskId, string unassignedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Where(t => t.Id == taskId)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                task.AssignedUserId = null;
                task.ModifiedBy = unassignedByUser;
                task.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} unassigned", taskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> BulkAssignTasksAsync(BulkAssignTasksRequestDto bulkAssignDto, string assignedByUser)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Where(t => bulkAssignDto.TaskIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var task in tasks)
                {
                    task.AssignedUserId = bulkAssignDto.AssignedUserId;
                    task.ModifiedBy = assignedByUser;
                    task.ModifiedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk assigned {Count} tasks to user {AssignedUserId}", tasks.Count, bulkAssignDto.AssignedUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk task assignment");
                throw;
            }
        }

        #endregion

        #region Task Statistics

        public async Task<TaskStatisticsDto> GetTaskStatisticsAsync(int? projectId = null)
        {
            try
            {
                var query = _context.ProjectTasks.AsQueryable();

                if (projectId.HasValue)
                    query = query.Where(t => t.ProjectId == projectId.Value);

                var totalTasks = await query.CountAsync();
                var overdueTasks = await query.CountAsync(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow);

                var priorityCounts = await query
                    .GroupBy(t => t.Priority ?? "none")
                    .Select(g => new { Priority = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Priority, x => x.Count);

                return new TaskStatisticsDto
                {
                    TotalTasks = totalTasks,
                    OverdueTasks = overdueTasks,
                    TasksByPriority = priorityCounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                throw;
            }
        }

        public async Task<bool> TaskExistsAsync(int id, bool isProjectTask = true)
        {
            if (isProjectTask)
                return await _context.ProjectTasks.AnyAsync(t => t.Id == id);
            else
                return await _context.DailyTasks.AnyAsync(t => t.Id == id);
        }

        #endregion

        #region Bulk Operations

        public async Task<bool> BulkUpdateTaskStatusAsync(BulkUpdateTaskStatusDto bulkUpdateDto, string updatedByUser)
        {
            try
            {
                if (bulkUpdateDto.TaskIds == null || !bulkUpdateDto.TaskIds.Any())
                    return false;

                // Find the column that matches the status/name
                var targetColumn = await _context.ProjectColumns
                    .Where(c => c.Name == bulkUpdateDto.Status)
                    .FirstOrDefaultAsync();

                if (targetColumn == null)
                {
                    _logger.LogWarning("Column with name {Status} not found for bulk status update", bulkUpdateDto.Status);
                    return false;
                }

                var tasks = await _context.ProjectTasks
                    .Where(t => bulkUpdateDto.TaskIds.Contains(t.Id))
                    .ToListAsync();

                if (!tasks.Any())
                    return false;

                var now = DateTime.UtcNow;
                foreach (var task in tasks)
                {
                    task.ColumnId = targetColumn.Id;
                    task.ModifiedDate = now;
                    task.ModifiedBy = updatedByUser;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating task status");
                throw;
            }
        }

        #endregion

        #region Private Mapping Methods

        private ProjectTaskResponseDto MapToProjectTaskDto(ProjectTask task)
        {
            // Get assigned user name - check both Users table and MainAdminUsers table
            string? assignedUserName = null;
            
            if (task.AssignedUserId.HasValue)
            {
                // First try the navigation property (Users table)
                if (task.AssignedUser != null)
                {
                    assignedUserName = $"{task.AssignedUser.FirstName} {task.AssignedUser.LastName}".Trim();
                }
                else
                {
                    // Fallback: check MainAdminUsers table for the assigned user
                    var mainAdmin = _context.MainAdminUsers
                        .Where(u => u.Id == task.AssignedUserId.Value)
                        .Select(u => new { u.FirstName, u.LastName })
                        .FirstOrDefault();
                    
                    if (mainAdmin != null)
                    {
                        assignedUserName = $"{mainAdmin.FirstName} {mainAdmin.LastName}".Trim();
                    }
                }
            }

            return new ProjectTaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                ProjectId = task.ProjectId,
                ProjectName = task.Project?.Name ?? string.Empty,
                ColumnId = task.ColumnId,
                ColumnName = task.Column?.Name ?? string.Empty,
                ColumnColor = task.Column?.Color,
                Priority = task.Priority,
                DueDate = task.DueDate,
                AssignedUserId = task.AssignedUserId,
                AssignedUserName = assignedUserName,
                DisplayOrder = task.DisplayOrder,
                CreatedDate = task.CreatedDate,
                CreatedBy = task.CreatedBy,
                ModifiedDate = task.ModifiedDate,
                ModifiedBy = task.ModifiedBy,
                CommentsCount = task.Comments?.Count ?? 0,
                AttachmentsCount = task.TaskAttachments?.Count ?? 0
            };
        }

        private DailyTaskResponseDto MapToDailyTaskDto(DailyTask task)
        {
            // Derive status from Status field, with fallback logic for IsCompleted
            var status = task.Status ?? "todo";
            if (task.IsCompleted && status != "done")
            {
                status = "done";
            }

            // Get assigned user name - check both Users table and MainAdminUsers table
            string? assignedUserName = null;
            
            if (task.AssignedUserId.HasValue)
            {
                // First try the navigation property (Users table)
                if (task.AssignedUser != null)
                {
                    assignedUserName = $"{task.AssignedUser.FirstName} {task.AssignedUser.LastName}".Trim();
                }
                else
                {
                    // Fallback: check MainAdminUsers table for the assigned user
                    var mainAdmin = _context.MainAdminUsers
                        .Where(u => u.Id == task.AssignedUserId.Value)
                        .Select(u => new { u.FirstName, u.LastName })
                        .FirstOrDefault();
                    
                    if (mainAdmin != null)
                    {
                        assignedUserName = $"{mainAdmin.FirstName} {mainAdmin.LastName}".Trim();
                    }
                }
            }

            return new DailyTaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                CompletedDate = task.CompletedDate,
                AssignedUserId = task.AssignedUserId,
                AssignedUserName = assignedUserName,
                Priority = task.Priority,
                Status = status,
                CreatedDate = task.CreatedDate,
                CreatedBy = task.CreatedBy
            };
        }

        #endregion
    }
}
