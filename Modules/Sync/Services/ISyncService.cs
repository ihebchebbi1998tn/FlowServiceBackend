using System.Threading.Tasks;
using MyApi.Modules.Sync.DTOs;

namespace MyApi.Modules.Sync.Services
{
    public interface ISyncService
    {
        Task<SyncPushResponseDto> PushAsync(SyncPushRequestDto request, string currentUser);
        Task<SyncPullResponseDto> PullAsync(string? cursor, int limit);
        Task<SyncHistoryResponseDto> GetHistoryAsync(SyncHistoryQueryDto query, string currentUser, bool isAdmin);
        Task<SyncPushResultDto> RetryAsync(SyncRetryRequestDto request, string currentUser, bool isAdmin);
    }
}
