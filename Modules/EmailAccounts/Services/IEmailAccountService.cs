using MyApi.Modules.EmailAccounts.DTOs;

namespace MyApi.Modules.EmailAccounts.Services
{
    public interface IEmailAccountService
    {
        // OAuth configuration (tells frontend where to redirect for consent)
        Task<OAuthConfigDto> GetOAuthConfigAsync(string provider);

        // OAuth callback (exchange code for tokens, create connected account)
        Task<ConnectedEmailAccountDto> HandleOAuthCallbackAsync(int userId, OAuthCallbackDto callbackDto);

        // Connected accounts CRUD
        Task<IEnumerable<ConnectedEmailAccountDto>> GetAccountsByUserAsync(int userId);
        Task<ConnectedEmailAccountDto?> GetAccountByIdAsync(Guid id);
        Task<bool> DisconnectAccountAsync(Guid id, int userId);

        // Reconnect (re-trigger OAuth for an existing account)
        Task<ConnectedEmailAccountDto?> ReconnectAccountAsync(Guid id, int userId, OAuthCallbackDto callbackDto);

        // Email settings
        Task<ConnectedEmailAccountDto?> UpdateEmailSettingsAsync(Guid accountId, int userId, UpdateEmailSettingsDto dto);

        // Calendar settings
        Task<ConnectedEmailAccountDto?> UpdateCalendarSettingsAsync(Guid accountId, int userId, UpdateCalendarSettingsDto dto);

        // Blocklist
        Task<BlocklistItemDto?> AddBlocklistItemAsync(Guid accountId, int userId, CreateBlocklistItemDto dto);
        Task<bool> RemoveBlocklistItemAsync(Guid accountId, Guid itemId, int userId);
        Task<IEnumerable<BlocklistItemDto>> GetBlocklistAsync(Guid accountId, int userId);

        // ─── Email Sync & Fetch ───

        /// <summary>
        /// Trigger a sync of emails from the provider (Gmail/Outlook) for a connected account.
        /// Fetches recent emails and stores them in SyncedEmails table.
        /// </summary>
        Task<SyncResultDto> SyncEmailsAsync(Guid accountId, int userId, int maxResults = 50);

        /// <summary>
        /// Get synced emails for a connected account with pagination.
        /// </summary>
        Task<SyncedEmailsPageDto> GetSyncedEmailsAsync(Guid accountId, int userId, int page = 1, int pageSize = 25, string? search = null);
    }
}
