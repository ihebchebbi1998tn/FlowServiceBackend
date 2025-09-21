using MyApi.DTOs;

namespace MyApi.Services
{
    public interface IContactTagService
    {
        Task<ContactTagListResponseDto> GetAllTagsAsync();
        Task<ContactTagDto?> GetTagByIdAsync(int id);
        Task<ContactTagDto> CreateTagAsync(CreateContactTagRequestDto createDto);
        Task<ContactTagDto?> UpdateTagAsync(int id, UpdateContactTagRequestDto updateDto);
        Task<bool> DeleteTagAsync(int id);
        Task<bool> TagExistsAsync(string name);
    }
}