using AuthService.Domain.DTOs;

namespace AuthService.Domain.Interfaces.Repositories
{
    public interface IApiKeyCacheService
    {
        Task<CachedApiKeyDTO?> GetCachedApiKeyAsync(string prefix, CancellationToken cancellationToken = default);
        Task SetCachedApiKeyAsync(string prefix, CachedApiKeyDTO apiKey, CancellationToken cancellationToken = default);
        Task RemoveCachedApiKeyAsync(string prefix, CancellationToken cancellationToken = default);
    }
}
