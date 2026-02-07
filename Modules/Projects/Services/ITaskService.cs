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
        Task<List<DailyTaskResponseDto>> GetDailyTasksByDateAsync(DateTime date);
        Task<DailyTaskResponseDto?> GetDailyTaskByIdAsync(int id);
        Task<DailyTaskResponseDto> CreateDailyTaskAsync(CreateDailyTaskRequestDto createDto, string createdByUser);
        Task<DailyTaskResponseDto?> UpdateDailyTaskAsync(int id, UpdateDailyTaskRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteDailyTaskAsync(int id, string deletedByUser);
        Task<bool> CompleteDailyTaskAsync(int id, string completedByUser);

        // Task search and filtering
        Task<TaskListResponseDto> SearchTasksAsync(TaskSearchRequestDto searchRequest);
        Task<List<ProjectTaskResponseDto>> GetTasksByAssigneeAsync(int assigneeId, int? projectId = null);
        Task<List<ProjectTaskResponseDto>> GetOverdueTasksAsync(int? projectId = null, int? assigneeId = null);

        // Task movement and positioning
        Task<bool> MoveTaskAsync(int taskId, MoveTaskRequestDto moveDto, string movedByUser);
        Task<bool> BulkMoveTasksAsync(BulkMoveTasksRequestDto bulkMoveDto, string movedByUser);
        Task<int> GetNextTaskDisplayOrderAsync(int columnId);
        Task<bool> ReorderTasksInColumnAsync(int columnId, List<int> taskIds, string updatedByUser);

        // Task assignment
        Task<bool> AssignTaskAsync(int taskId, AssignTaskRequestDto assignDto, string assignedByUser);
        Task<bool> UnassignTaskAsync(int taskId, string unassignedByUser);
        Task<bool> BulkAssignTasksAsync(BulkAssignTasksRequestDto bulkAssignDto, string assignedByUser);

        // Bulk operations
        Task<bool> BulkUpdateTaskStatusAsync(BulkUpdateTaskStatusDto bulkUpdateDto, string updatedByUser);

        // Task statistics
        Task<TaskStatisticsDto> GetTaskStatisticsAsync(int? projectId = null);
        Task<bool> TaskExistsAsync(int id, bool isProjectTask = true);
    }
}
