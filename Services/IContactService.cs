using MyApi.DTOs;

namespace MyApi.Services
{
    public interface IContactService
    {
        Task<ContactListResponseDto> GetAllContactsAsync(ContactSearchRequestDto? searchRequest = null);
        Task<ContactResponseDto?> GetContactByIdAsync(int id);
        Task<ContactResponseDto> CreateContactAsync(CreateContactRequestDto createDto, string createdByUser);
        Task<ContactResponseDto?> UpdateContactAsync(int id, UpdateContactRequestDto updateDto, string modifiedByUser);
        Task<bool> DeleteContactAsync(int id, string deletedByUser);
        Task<bool> ContactExistsAsync(string email);
        Task<BulkImportResultDto> BulkImportContactsAsync(BulkImportContactRequestDto importRequest, string createdByUser);
        Task<bool> AssignTagToContactAsync(int contactId, int tagId, string assignedByUser);
        Task<bool> RemoveTagFromContactAsync(int contactId, int tagId);
        Task<ContactListResponseDto> SearchContactsAsync(string searchTerm, int pageNumber = 1, int pageSize = 20);
    }
}