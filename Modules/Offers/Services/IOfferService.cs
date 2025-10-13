using MyApi.Modules.Offers.DTOs;

namespace MyApi.Modules.Offers.Services
{
    public interface IOfferService
    {
        Task<PaginatedOfferResponse> GetOffersAsync(
            string? status = null,
            string? category = null,
            string? source = null,
            string? contactId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? search = null,
            int page = 1,
            int limit = 20,
            string sortBy = "updated_at",
            string sortOrder = "desc"
        );
        
        Task<OfferDto?> GetOfferByIdAsync(string id);
        Task<OfferDto> CreateOfferAsync(CreateOfferDto createDto, string userId);
        Task<OfferDto> UpdateOfferAsync(string id, UpdateOfferDto updateDto, string userId);
        Task<bool> DeleteOfferAsync(string id);
        Task<OfferDto> RenewOfferAsync(string id, string userId);
        Task<object> ConvertOfferAsync(string id, ConvertOfferDto convertDto, string userId);
        Task<OfferStatsDto> GetOfferStatsAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
        
        // Offer Items
        Task<OfferItemDto> AddOfferItemAsync(string offerId, CreateOfferItemDto itemDto);
        Task<OfferItemDto> UpdateOfferItemAsync(string offerId, string itemId, CreateOfferItemDto itemDto);
        Task<bool> DeleteOfferItemAsync(string offerId, string itemId);
        
        // Offer Activities
        Task<List<OfferActivityDto>> GetOfferActivitiesAsync(string offerId, string? type = null, int page = 1, int limit = 20);
    }
}
