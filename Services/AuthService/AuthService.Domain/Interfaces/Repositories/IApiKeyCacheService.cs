using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces.Repositories
{
    public interface IApiKeyCacheService
    {
        Task<ApiKey?> GetCachedApiKeyAsync(string prefix, CancellationToken cancellationToken = default);
        Task SetCachedApiKeyAsync(string prefix, ApiKey apiKey, CancellationToken cancellationToken = default);
        Task RemoveCachedApiKeyAsync(string prefix, CancellationToken cancellationToken = default);
    }
}
