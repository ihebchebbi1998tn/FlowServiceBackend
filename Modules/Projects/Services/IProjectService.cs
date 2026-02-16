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
        
        // Project statistics
        Task<ProjectStatisticsDto> GetStatisticsAsync();
        
        // Project search
        Task<List<ProjectResponseDto>> SearchProjectsAsync(string searchTerm);
    }
}
