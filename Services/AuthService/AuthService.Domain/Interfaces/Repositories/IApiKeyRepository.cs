using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces.Repositories
{
    public interface IApiKeyRepository
    {
        Task<ApiKey?> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
        Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ApiKey?> GetByHashAsync(string hash, CancellationToken cancellationToken = default);
        Task<IEnumerable<ApiKey>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
        Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
