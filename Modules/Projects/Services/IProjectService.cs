using MyApi.Modules.Projects.DTOs;

namespace MyApi.Modules.Projects.Services
{
    public interface IProjectService
    {
        // Project CRUD operations
        Task<ProjectListResponseDto> GetAllProjectsAsync(ProjectSearchRequestDto? searchRequest = null);
        Task<ProjectResponseDto?> GetProjectByIdAsync(int id);
        Task<ProjectResponseDto> CreateProjectAsync(CreateProjectRequestDto createDto, string createdByUser);
        Task<ProjectResponseDto?> UpdateProjectAsync(int id, UpdateProjectRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteProjectAsync(int id, string deletedByUser);
        
        // Project search and filtering
        Task<ProjectListResponseDto> SearchProjectsAsync(string searchTerm, int pageNumber = 1, int pageSize = 20);
        Task<ProjectListResponseDto> GetProjectsByOwnerAsync(int ownerId, int pageNumber = 1, int pageSize = 20);
        Task<ProjectListResponseDto> GetProjectsByContactAsync(int contactId, int pageNumber = 1, int pageSize = 20);
        Task<ProjectListResponseDto> GetProjectsByTeamMemberAsync(int userId, int pageNumber = 1, int pageSize = 20);
        
        // Project team management
        Task<bool> AssignTeamMemberAsync(int projectId, AssignTeamMemberDto assignDto, string assignedByUser);
        Task<bool> RemoveTeamMemberAsync(int projectId, RemoveTeamMemberDto removeDto, string removedByUser);
        Task<List<int>> GetProjectTeamMembersAsync(int projectId);
        
        // Project statistics
        Task<ProjectStatsDto> GetProjectStatsAsync(int projectId);
        Task<Dictionary<int, ProjectStatsDto>> GetMultipleProjectStatsAsync(List<int> projectIds);
        
        // Bulk operations
        Task<bool> BulkUpdateProjectStatusAsync(BulkUpdateProjectStatusDto bulkUpdateDto, string updatedByUser);
        Task<bool> BulkArchiveProjectsAsync(List<int> projectIds, bool archive, string updatedByUser);
        
        // Project validation
        Task<bool> ProjectExistsAsync(int id);
        Task<bool> UserCanAccessProjectAsync(int projectId, int userId);
        Task<bool> UserIsProjectOwnerAsync(int projectId, int userId);
        Task<bool> UserIsProjectTeamMemberAsync(int projectId, int userId);
    }
}
