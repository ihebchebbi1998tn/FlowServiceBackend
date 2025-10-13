using MyApi.Data;
using MyApi.Modules.Projects.DTOs;
using MyApi.Modules.Projects.Models;
using MyApi.Modules.Contacts.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MyApi.Modules.Projects.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Project Tasks

        public async Task<List<ProjectTaskResponseDto>> GetProjectTasksAsync(int projectId)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.Contact)
                    .Include(t => t.ParentTask)
                    .Where(t => t.ProjectId == projectId && !t.IsDeleted)
                    .OrderBy(t => t.Column.Position)
                    .ThenBy(t => t.Position)
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
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.Contact)
                    .Include(t => t.ParentTask)
                    .Where(t => t.ColumnId == columnId && !t.IsDeleted)
                    .OrderBy(t => t.Position)
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
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.Contact)
                    .Include(t => t.ParentTask)
                    .Include(t => t.SubTasks.Where(st => !st.IsDeleted))
                    .Where(t => t.Id == id && !t.IsDeleted)
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

                // Get next position in column
                var position = await GetNextTaskPositionAsync(createDto.ColumnId);

                var task = new ProjectTask
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    ProjectId = createDto.ProjectId,
                    ContactId = createDto.ContactId,
                    AssigneeId = createDto.AssigneeId,
                    AssigneeName = createDto.AssigneeName,
                    Status = createDto.Status,
                    Priority = createDto.Priority,
                    ColumnId = createDto.ColumnId,
                    Position = position,
                    ParentTaskId = createDto.ParentTaskId,
                    DueDate = createDto.DueDate,
                    StartDate = createDto.StartDate,
                    EstimatedHours = createDto.EstimatedHours,
                    Tags = JsonSerializer.Serialize(createDto.Tags),
                    CreatedBy = createdByUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return null;

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Title))
                    task.Title = updateDto.Title;

                if (updateDto.Description != null)
                    task.Description = updateDto.Description;

                if (updateDto.ContactId.HasValue)
                    task.ContactId = updateDto.ContactId.Value;

                if (updateDto.AssigneeId.HasValue)
                    task.AssigneeId = updateDto.AssigneeId.Value;

                if (!string.IsNullOrEmpty(updateDto.AssigneeName))
                    task.AssigneeName = updateDto.AssigneeName;

                if (!string.IsNullOrEmpty(updateDto.Status))
                    task.Status = updateDto.Status;

                if (!string.IsNullOrEmpty(updateDto.Priority))
                    task.Priority = updateDto.Priority;

                if (updateDto.ColumnId.HasValue)
                    task.ColumnId = updateDto.ColumnId.Value;

                if (updateDto.Position.HasValue)
                    task.Position = updateDto.Position.Value;

                if (updateDto.ParentTaskId.HasValue)
                    task.ParentTaskId = updateDto.ParentTaskId.Value;

                if (updateDto.DueDate.HasValue)
                    task.DueDate = updateDto.DueDate.Value;

                if (updateDto.StartDate.HasValue)
                    task.StartDate = updateDto.StartDate.Value;

                if (updateDto.EstimatedHours.HasValue)
                    task.EstimatedHours = updateDto.EstimatedHours.Value;

                if (updateDto.ActualHours.HasValue)
                    task.ActualHours = updateDto.ActualHours.Value;

                if (updateDto.Tags != null)
                    task.Tags = JsonSerializer.Serialize(updateDto.Tags);

                if (updateDto.CompletedAt.HasValue)
                    task.CompletedAt = updateDto.CompletedAt.Value;

                task.ModifiedBy = modifiedByUser;
                task.UpdatedAt = DateTime.UtcNow;

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
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                // Soft delete
                task.IsDeleted = true;
                task.ModifiedBy = deletedByUser;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Project task soft deleted successfully with ID {TaskId}", id);
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
                    .Where(t => t.UserId == userId && !t.IsDeleted)
                    .OrderBy(t => t.Position)
                    .ToListAsync();

                return tasks.Select(MapToDailyTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily tasks for user {UserId}", userId);
                throw;
            }
        }

        public async Task<DailyTaskResponseDto?> GetDailyTaskByIdAsync(int id)
        {
            try
            {
                var task = await _context.DailyTasks
                    .Where(t => t.Id == id && !t.IsDeleted)
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
                // Get next position for user
                var position = await GetNextDailyTaskPositionAsync(createDto.UserId);

                var task = new DailyTask
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    UserId = createDto.UserId,
                    UserName = createDto.UserName,
                    Status = createDto.Status,
                    Priority = createDto.Priority,
                    Position = position,
                    DueDate = createDto.DueDate,
                    EstimatedHours = createDto.EstimatedHours,
                    Tags = JsonSerializer.Serialize(createDto.Tags),
                    CreatedBy = createdByUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return null;

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Title))
                    task.Title = updateDto.Title;

                if (updateDto.Description != null)
                    task.Description = updateDto.Description;

                if (!string.IsNullOrEmpty(updateDto.Status))
                    task.Status = updateDto.Status;

                if (!string.IsNullOrEmpty(updateDto.Priority))
                    task.Priority = updateDto.Priority;

                if (updateDto.Position.HasValue)
                    task.Position = updateDto.Position.Value;

                if (updateDto.DueDate.HasValue)
                    task.DueDate = updateDto.DueDate.Value;

                if (updateDto.EstimatedHours.HasValue)
                    task.EstimatedHours = updateDto.EstimatedHours.Value;

                if (updateDto.ActualHours.HasValue)
                    task.ActualHours = updateDto.ActualHours.Value;

                if (updateDto.Tags != null)
                    task.Tags = JsonSerializer.Serialize(updateDto.Tags);

                if (updateDto.CompletedAt.HasValue)
                    task.CompletedAt = updateDto.CompletedAt.Value;

                task.ModifiedBy = modifiedByUser;
                task.UpdatedAt = DateTime.UtcNow;

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
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                // Soft delete
                task.IsDeleted = true;
                task.ModifiedBy = deletedByUser;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Daily task soft deleted successfully with ID {TaskId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting daily task with ID {TaskId}", id);
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
                    .Include(t => t.Contact)
                    .Where(t => !t.IsDeleted);

                // Apply filters for project tasks
                if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
                {
                    var searchTerm = searchRequest.SearchTerm.ToLower();
                    projectTasksQuery = projectTasksQuery.Where(t => 
                        t.Title.ToLower().Contains(searchTerm) ||
                        (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(searchRequest.Status))
                    projectTasksQuery = projectTasksQuery.Where(t => t.Status == searchRequest.Status);

                if (!string.IsNullOrEmpty(searchRequest.Priority))
                    projectTasksQuery = projectTasksQuery.Where(t => t.Priority == searchRequest.Priority);

                if (searchRequest.ProjectId.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.ProjectId == searchRequest.ProjectId.Value);

                if (searchRequest.AssigneeId.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.AssigneeId == searchRequest.AssigneeId.Value);

                if (searchRequest.ContactId.HasValue)
                    projectTasksQuery = projectTasksQuery.Where(t => t.ContactId == searchRequest.ContactId.Value);

                // Apply pagination
                var totalCount = await projectTasksQuery.CountAsync();
                var skip = (searchRequest.PageNumber - 1) * searchRequest.PageSize;

                var projectTasks = await projectTasksQuery
                    .Skip(skip)
                    .Take(searchRequest.PageSize)
                    .ToListAsync();

                var projectTaskDtos = projectTasks.Select(MapToProjectTaskDto).ToList();
                
                // For now, we'll return empty daily tasks in search results
                // You can extend this to include daily task search if needed
                var dailyTaskDtos = new List<DailyTaskResponseDto>();

                return new TaskListResponseDto
                {
                    ProjectTasks = projectTaskDtos,
                    DailyTasks = dailyTaskDtos,
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
                    .Include(t => t.Contact)
                    .Where(t => t.AssigneeId == assigneeId && !t.IsDeleted);

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
                    .Include(t => t.Contact)
                    .Where(t => !t.IsDeleted && 
                               t.DueDate.HasValue && 
                               t.DueDate.Value < DateTime.UtcNow && 
                               !t.CompletedAt.HasValue);

                if (projectId.HasValue)
                    query = query.Where(t => t.ProjectId == projectId.Value);

                if (assigneeId.HasValue)
                    query = query.Where(t => t.AssigneeId == assigneeId.Value);

                var tasks = await query.ToListAsync();
                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks");
                throw;
            }
        }

        public async Task<List<ProjectTaskResponseDto>> GetTasksByContactAsync(int contactId)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.Contact)
                    .Where(t => t.ContactId == contactId && !t.IsDeleted)
                    .ToListAsync();

                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by contact {ContactId}", contactId);
                throw;
            }
        }

        public async Task<List<ProjectTaskResponseDto>> GetSubTasksAsync(int parentTaskId)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.Contact)
                    .Where(t => t.ParentTaskId == parentTaskId && !t.IsDeleted)
                    .ToListAsync();

                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-tasks for parent {ParentTaskId}", parentTaskId);
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
                    .Where(t => t.Id == taskId && !t.IsDeleted)
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
                task.Position = moveDto.Position;
                task.ModifiedBy = movedByUser;
                task.UpdatedAt = DateTime.UtcNow;

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
                        Position = taskMove.Position
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

        public async Task<int> GetNextTaskPositionAsync(int columnId)
        {
            var maxPosition = await _context.ProjectTasks
                .Where(t => t.ColumnId == columnId && !t.IsDeleted)
                .MaxAsync(t => (int?)t.Position) ?? 0;

            return maxPosition + 1;
        }

        public async Task<bool> ReorderTasksInColumnAsync(int columnId, List<int> taskIds, string updatedByUser)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Where(t => taskIds.Contains(t.Id) && t.ColumnId == columnId && !t.IsDeleted)
                    .ToListAsync();

                for (int i = 0; i < taskIds.Count; i++)
                {
                    var task = tasks.FirstOrDefault(t => t.Id == taskIds[i]);
                    if (task != null)
                    {
                        task.Position = i + 1;
                        task.ModifiedBy = updatedByUser;
                        task.UpdatedAt = DateTime.UtcNow;
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
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                task.AssigneeId = assignDto.AssigneeId;
                task.AssigneeName = assignDto.AssigneeName;
                task.ModifiedBy = assignedByUser;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} assigned to user {AssigneeId}", taskId, assignDto.AssigneeId);
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
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                task.AssigneeId = null;
                task.AssigneeName = null;
                task.ModifiedBy = unassignedByUser;
                task.UpdatedAt = DateTime.UtcNow;

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
                    .Where(t => bulkAssignDto.TaskIds.Contains(t.Id) && !t.IsDeleted)
                    .ToListAsync();

                foreach (var task in tasks)
                {
                    task.AssigneeId = bulkAssignDto.AssigneeId;
                    task.AssigneeName = bulkAssignDto.AssigneeName;
                    task.ModifiedBy = assignedByUser;
                    task.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk assigned {Count} tasks to user {AssigneeId}", tasks.Count, bulkAssignDto.AssigneeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk task assignment");
                throw;
            }
        }

        #endregion

        #region Task Status Management

        public async Task<bool> UpdateTaskStatusAsync(int taskId, string status, string updatedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                task.Status = status;
                task.ModifiedBy = updatedByUser;
                task.UpdatedAt = DateTime.UtcNow;

                // If status indicates completion, set completed date
                if (status.ToLower() == "done" || status.ToLower() == "completed")
                {
                    task.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} status updated to {Status}", taskId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId} status", taskId);
                throw;
            }
        }

        public async Task<bool> CompleteTaskAsync(int taskId, string completedByUser)
        {
            return await UpdateTaskStatusAsync(taskId, "done", completedByUser);
        }

        public async Task<bool> BulkUpdateTaskStatusAsync(BulkUpdateTaskStatusDto bulkUpdateDto, string updatedByUser)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Where(t => bulkUpdateDto.TaskIds.Contains(t.Id) && !t.IsDeleted)
                    .ToListAsync();

                foreach (var task in tasks)
                {
                    task.Status = bulkUpdateDto.Status;
                    task.ModifiedBy = updatedByUser;
                    task.UpdatedAt = DateTime.UtcNow;

                    // If status indicates completion, set completed date
                    if (bulkUpdateDto.Status.ToLower() == "done" || bulkUpdateDto.Status.ToLower() == "completed")
                    {
                        task.CompletedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk updated status for {Count} tasks to {Status}", tasks.Count, bulkUpdateDto.Status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk task status update");
                throw;
            }
        }

        #endregion

        #region Validation and Utilities

        public async Task<bool> ProjectTaskExistsAsync(int id)
        {
            return await _context.ProjectTasks.AnyAsync(t => t.Id == id && !t.IsDeleted);
        }

        public async Task<bool> DailyTaskExistsAsync(int id)
        {
            return await _context.DailyTasks.AnyAsync(t => t.Id == id && !t.IsDeleted);
        }

        public async Task<bool> UserCanAccessTaskAsync(int taskId, int userId, bool isProjectTask = true)
        {
            if (isProjectTask)
            {
                var task = await _context.ProjectTasks
                    .Include(t => t.Project)
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                // Owner, assignee, or team member can access
                return task.Project.OwnerId == userId || 
                       task.AssigneeId == userId ||
                       (!string.IsNullOrEmpty(task.Project.TeamMembers) && 
                        task.Project.TeamMembers.Contains(userId.ToString()));
            }
            else
            {
                var task = await _context.DailyTasks
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                return task?.UserId == userId;
            }
        }

        public async Task<bool> TaskBelongsToProjectAsync(int taskId, int projectId)
        {
            return await _context.ProjectTasks
                .AnyAsync(t => t.Id == taskId && t.ProjectId == projectId && !t.IsDeleted);
        }

        public async Task<bool> TaskBelongsToColumnAsync(int taskId, int columnId)
        {
            return await _context.ProjectTasks
                .AnyAsync(t => t.Id == taskId && t.ColumnId == columnId && !t.IsDeleted);
        }

        #endregion

        #region Statistics

        public async Task<Dictionary<string, int>> GetTaskStatusCountsAsync(int projectId)
        {
            try
            {
                var statusCounts = await _context.ProjectTasks
                    .Where(t => t.ProjectId == projectId && !t.IsDeleted)
                    .GroupBy(t => t.Status)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                return statusCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task status counts for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetUserTaskStatusCountsAsync(int userId)
        {
            try
            {
                var projectTaskCounts = await _context.ProjectTasks
                    .Where(t => t.AssigneeId == userId && !t.IsDeleted)
                    .GroupBy(t => t.Status)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                var dailyTaskCounts = await _context.DailyTasks
                    .Where(t => t.UserId == userId && !t.IsDeleted)
                    .GroupBy(t => t.Status)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                // Combine both counts
                var combinedCounts = new Dictionary<string, int>();
                
                foreach (var kvp in projectTaskCounts)
                {
                    combinedCounts[kvp.Key] = kvp.Value;
                }

                foreach (var kvp in dailyTaskCounts)
                {
                    if (combinedCounts.ContainsKey(kvp.Key))
                        combinedCounts[kvp.Key] += kvp.Value;
                    else
                        combinedCounts[kvp.Key] = kvp.Value;
                }

                return combinedCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task status counts for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUserOverdueTaskCountAsync(int userId)
        {
            try
            {
                var projectTaskCount = await _context.ProjectTasks
                    .CountAsync(t => t.AssigneeId == userId && 
                               !t.IsDeleted &&
                               t.DueDate.HasValue && 
                               t.DueDate.Value < DateTime.UtcNow && 
                               !t.CompletedAt.HasValue);

                var dailyTaskCount = await _context.DailyTasks
                    .CountAsync(t => t.UserId == userId && 
                               !t.IsDeleted &&
                               t.DueDate.HasValue && 
                               t.DueDate.Value < DateTime.UtcNow && 
                               !t.CompletedAt.HasValue);

                return projectTaskCount + dailyTaskCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue task count for user {UserId}", userId);
                throw;
            }
        }

        public async Task<decimal> GetTaskCompletionPercentageAsync(int projectId)
        {
            try
            {
                var totalTasks = await _context.ProjectTasks
                    .CountAsync(t => t.ProjectId == projectId && !t.IsDeleted);

                if (totalTasks == 0)
                    return 0;

                var completedTasks = await _context.ProjectTasks
                    .CountAsync(t => t.ProjectId == projectId && !t.IsDeleted && t.CompletedAt.HasValue);

                return Math.Round((decimal)completedTasks / totalTasks * 100, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task completion percentage for project {ProjectId}", projectId);
                throw;
            }
        }

        #endregion

        #region Task Hierarchy

        public async Task<bool> CreateSubTaskAsync(int parentTaskId, CreateProjectTaskRequestDto createDto, string createdByUser)
        {
            try
            {
                createDto.ParentTaskId = parentTaskId;
                var subTask = await CreateProjectTaskAsync(createDto, createdByUser);
                return subTask != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-task for parent {ParentTaskId}", parentTaskId);
                throw;
            }
        }

        public async Task<bool> ConvertToSubTaskAsync(int taskId, int parentTaskId, string updatedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                // Validate parent task exists and is in same project
                var parentTask = await _context.ProjectTasks
                    .Where(t => t.Id == parentTaskId && t.ProjectId == task.ProjectId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (parentTask == null)
                    return false;

                task.ParentTaskId = parentTaskId;
                task.ModifiedBy = updatedByUser;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} converted to sub-task of {ParentTaskId}", taskId, parentTaskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting task {TaskId} to sub-task", taskId);
                throw;
            }
        }

        public async Task<bool> ConvertToStandaloneTaskAsync(int taskId, string updatedByUser)
        {
            try
            {
                var task = await _context.ProjectTasks
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return false;

                task.ParentTaskId = null;
                task.ModifiedBy = updatedByUser;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} converted to standalone task", taskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting task {TaskId} to standalone", taskId);
                throw;
            }
        }

        public async Task<List<ProjectTaskResponseDto>> GetTaskHierarchyAsync(int parentTaskId)
        {
            try
            {
                var tasks = await _context.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Column)
                    .Include(t => t.Contact)
                    .Include(t => t.SubTasks.Where(st => !st.IsDeleted))
                    .Where(t => t.ParentTaskId == parentTaskId && !t.IsDeleted)
                    .ToListAsync();

                return tasks.Select(MapToProjectTaskDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task hierarchy for parent {ParentTaskId}", parentTaskId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<int> GetNextDailyTaskPositionAsync(int userId)
        {
            var maxPosition = await _context.DailyTasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .MaxAsync(t => (int?)t.Position) ?? 0;

            return maxPosition + 1;
        }

        private ProjectTaskResponseDto MapToProjectTaskDto(ProjectTask task)
        {
            var tags = string.IsNullOrEmpty(task.Tags) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(task.Tags) ?? new List<string>();

            var attachments = string.IsNullOrEmpty(task.Attachments) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(task.Attachments) ?? new List<string>();

            var subTasks = task.SubTasks?.Where(st => !st.IsDeleted).Select(MapToProjectTaskDto).ToList() ?? new List<ProjectTaskResponseDto>();

            var commentsCount = _context.TaskComments.Count(c => c.ProjectTaskId == task.Id && !c.IsDeleted);
            var attachmentsCount = _context.TaskAttachments.Count(a => a.ProjectTaskId == task.Id && !a.IsDeleted);

            return new ProjectTaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                ProjectId = task.ProjectId,
                ProjectName = task.Project?.Name ?? string.Empty,
                ContactId = task.ContactId,
                ContactName = task.Contact?.Name,
                AssigneeId = task.AssigneeId,
                AssigneeName = task.AssigneeName,
                Status = task.Status,
                Priority = task.Priority,
                ColumnId = task.ColumnId,
                ColumnTitle = task.Column?.Title ?? string.Empty,
                ColumnColor = task.Column?.Color ?? string.Empty,
                Position = task.Position,
                ParentTaskId = task.ParentTaskId,
                ParentTaskTitle = task.ParentTask?.Title,
                DueDate = task.DueDate,
                StartDate = task.StartDate,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                Tags = tags,
                Attachments = attachments,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                CompletedAt = task.CompletedAt,
                CreatedBy = task.CreatedBy,
                ModifiedBy = task.ModifiedBy,
                SubTasks = subTasks,
                CommentsCount = commentsCount,
                AttachmentsCount = attachmentsCount
            };
        }

        private DailyTaskResponseDto MapToDailyTaskDto(DailyTask task)
        {
            var tags = string.IsNullOrEmpty(task.Tags) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(task.Tags) ?? new List<string>();

            var attachments = string.IsNullOrEmpty(task.Attachments) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(task.Attachments) ?? new List<string>();

            var commentsCount = _context.TaskComments.Count(c => c.DailyTaskId == task.Id && !c.IsDeleted);
            var attachmentsCount = _context.TaskAttachments.Count(a => a.DailyTaskId == task.Id && !a.IsDeleted);

            return new DailyTaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                UserId = task.UserId,
                UserName = task.UserName,
                Status = task.Status,
                Priority = task.Priority,
                Position = task.Position,
                DueDate = task.DueDate,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                Tags = tags,
                Attachments = attachments,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                CompletedAt = task.CompletedAt,
                CreatedBy = task.CreatedBy,
                ModifiedBy = task.ModifiedBy,
                CommentsCount = commentsCount,
                AttachmentsCount = attachmentsCount
            };
        }

        #endregion
    }
}
