using MyApi.Modules.Lookups.DTOs;

namespace MyApi.Modules.Lookups.Services
{
    public interface ILookupService
    {
        // Article Categories
        Task<LookupListResponseDto> GetArticleCategoriesAsync();
        Task<LookupItemDto?> GetArticleCategoryByIdAsync(string id);
        Task<LookupItemDto> CreateArticleCategoryAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateArticleCategoryAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteArticleCategoryAsync(string id, string deletedByUser);

        // Article Statuses
        Task<LookupListResponseDto> GetArticleStatusesAsync();
        Task<LookupItemDto?> GetArticleStatusByIdAsync(string id);
        Task<LookupItemDto> CreateArticleStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateArticleStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteArticleStatusAsync(string id, string deletedByUser);

        // Service Categories
        Task<LookupListResponseDto> GetServiceCategoriesAsync();
        Task<LookupItemDto?> GetServiceCategoryByIdAsync(string id);
        Task<LookupItemDto> CreateServiceCategoryAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateServiceCategoryAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteServiceCategoryAsync(string id, string deletedByUser);

        // Task Statuses
        Task<LookupListResponseDto> GetTaskStatusesAsync();
        Task<LookupItemDto?> GetTaskStatusByIdAsync(string id);
        Task<LookupItemDto> CreateTaskStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateTaskStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteTaskStatusAsync(string id, string deletedByUser);

        // Event Types
        Task<LookupListResponseDto> GetEventTypesAsync();
        Task<LookupItemDto?> GetEventTypeByIdAsync(string id);
        Task<LookupItemDto> CreateEventTypeAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateEventTypeAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteEventTypeAsync(string id, string deletedByUser);

        // Priorities
        Task<LookupListResponseDto> GetPrioritiesAsync();
        Task<LookupItemDto?> GetPriorityByIdAsync(string id);
        Task<LookupItemDto> CreatePriorityAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdatePriorityAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeletePriorityAsync(string id, string deletedByUser);

        // Technician Statuses
        Task<LookupListResponseDto> GetTechnicianStatusesAsync();
        Task<LookupItemDto?> GetTechnicianStatusByIdAsync(string id);
        Task<LookupItemDto> CreateTechnicianStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateTechnicianStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteTechnicianStatusAsync(string id, string deletedByUser);

        // Leave Types
        Task<LookupListResponseDto> GetLeaveTypesAsync();
        Task<LookupItemDto?> GetLeaveTypeByIdAsync(string id);
        Task<LookupItemDto> CreateLeaveTypeAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateLeaveTypeAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteLeaveTypeAsync(string id, string deletedByUser);

        // Project Statuses
        Task<LookupListResponseDto> GetProjectStatusesAsync();
        Task<LookupItemDto?> GetProjectStatusByIdAsync(string id);
        Task<LookupItemDto> CreateProjectStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateProjectStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteProjectStatusAsync(string id, string deletedByUser);

        // Project Types
        Task<LookupListResponseDto> GetProjectTypesAsync();
        Task<LookupItemDto?> GetProjectTypeByIdAsync(string id);
        Task<LookupItemDto> CreateProjectTypeAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateProjectTypeAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteProjectTypeAsync(string id, string deletedByUser);

        // Offer Statuses
        Task<LookupListResponseDto> GetOfferStatusesAsync();
        Task<LookupItemDto?> GetOfferStatusByIdAsync(string id);
        Task<LookupItemDto> CreateOfferStatusAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateOfferStatusAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteOfferStatusAsync(string id, string deletedByUser);

        // Skills
        Task<LookupListResponseDto> GetSkillsAsync();
        Task<LookupItemDto?> GetSkillByIdAsync(string id);
        Task<LookupItemDto> CreateSkillAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateSkillAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteSkillAsync(string id, string deletedByUser);

        // Countries
        Task<LookupListResponseDto> GetCountriesAsync();
        Task<LookupItemDto?> GetCountryByIdAsync(string id);
        Task<LookupItemDto> CreateCountryAsync(CreateLookupItemRequestDto createDto, string createdByUser);
        Task<LookupItemDto?> UpdateCountryAsync(string id, UpdateLookupItemRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteCountryAsync(string id, string deletedByUser);

        // Currencies
        Task<CurrencyListResponseDto> GetCurrenciesAsync();
        Task<CurrencyDto?> GetCurrencyByIdAsync(string id);
        Task<CurrencyDto> CreateCurrencyAsync(CreateCurrencyRequestDto createDto, string createdByUser);
        Task<CurrencyDto?> UpdateCurrencyAsync(string id, UpdateCurrencyRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteCurrencyAsync(string id, string deletedByUser);
    }
}
