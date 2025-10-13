using MyApi.Modules.Projects.DTOs;

namespace MyApi.Modules.Projects.Services
{
    public interface ITaskService
    {
        // Project Task CRUD operations
        Task<List<ProjectTaskResponseDto>> GetProjectTasksAsync(int projectId);
        Task<List<ProjectTaskResponseDto>> GetColumnTasksAsync(int columnId);
        Task<ProjectTaskResponseDto?> GetProjectTaskByIdAsync(int id);
        Task<ProjectTaskResponseDto> CreateProjectTaskAsync(CreateProjectTaskRequestDto createDto, string createdByUser);
        Task<ProjectTaskResponseDto?> UpdateProjectTaskAsync(int id, UpdateProjectTaskRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteProjectTaskAsync(int id, string deletedByUser);

        // Daily Task CRUD operations
        Task<List<DailyTaskResponseDto>> GetUserDailyTasksAsync(int userId);
        Task<DailyTaskResponseDto?> GetDailyTaskByIdAsync(int id);
        Task<DailyTaskResponseDto> CreateDailyTaskAsync(CreateDailyTaskRequestDto createDto, string createdByUser);
        Task<DailyTaskResponseDto?> UpdateDailyTaskAsync(int id, UpdateDailyTaskRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteDailyTaskAsync(int id, string deletedByUser);

        // Task search and filtering
        Task<TaskListResponseDto> SearchTasksAsync(TaskSearchRequestDto searchRequest);
        Task<List<ProjectTaskResponseDto>> GetTasksByAssigneeAsync(int assigneeId, int? projectId = null);
        Task<List<ProjectTaskResponseDto>> GetOverdueTasksAsync(int? projectId = null, int? assigneeId = null);
        Task<List<ProjectTaskResponseDto>> GetTasksByContactAsync(int contactId);
        Task<List<ProjectTaskResponseDto>> GetSubTasksAsync(int parentTaskId);

        // Task movement and positioning
        Task<bool> MoveTaskAsync(int taskId, MoveTaskRequestDto moveDto, string movedByUser);
        Task<bool> BulkMoveTasksAsync(BulkMoveTasksRequestDto bulkMoveDto, string movedByUser);
        Task<int> GetNextTaskPositionAsync(int columnId);
        Task<bool> ReorderTasksInColumnAsync(int columnId, List<int> taskIds, string updatedByUser);

        // Task assignment
        Task<bool> AssignTaskAsync(int taskId, AssignTaskRequestDto assignDto, string assignedByUser);
        Task<bool> UnassignTaskAsync(int taskId, string unassignedByUser);
        Task<bool> BulkAssignTasksAsync(BulkAssignTasksRequestDto bulkAssignDto, string assignedByUser);

        // Task status management
        Task<bool> UpdateTaskStatusAsync(int taskId, string status, string updatedByUser);
        Task<bool> CompleteTaskAsync(int taskId, string completedByUser);
        Task<bool> BulkUpdateTaskStatusAsync(BulkUpdateTaskStatusDto bulkUpdateDto, string updatedByUser);

        // Task validation and utilities
        Task<bool> ProjectTaskExistsAsync(int id);
        Task<bool> DailyTaskExistsAsync(int id);
        Task<bool> UserCanAccessTaskAsync(int taskId, int userId, bool isProjectTask = true);
        Task<bool> TaskBelongsToProjectAsync(int taskId, int projectId);
        Task<bool> TaskBelongsToColumnAsync(int taskId, int columnId);

        // Task statistics
        Task<Dictionary<string, int>> GetTaskStatusCountsAsync(int projectId);
        Task<Dictionary<string, int>> GetUserTaskStatusCountsAsync(int userId);
        Task<int> GetUserOverdueTaskCountAsync(int userId);
        Task<decimal> GetTaskCompletionPercentageAsync(int projectId);

        // Task hierarchy (parent/child relationships)
        Task<bool> CreateSubTaskAsync(int parentTaskId, CreateProjectTaskRequestDto createDto, string createdByUser);
        Task<bool> ConvertToSubTaskAsync(int taskId, int parentTaskId, string updatedByUser);
        Task<bool> ConvertToStandaloneTaskAsync(int taskId, string updatedByUser);
        Task<List<ProjectTaskResponseDto>> GetTaskHierarchyAsync(int parentTaskId);
    }
}
