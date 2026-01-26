using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories
{
    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly AuthDbContext _context;

        public ApiKeyRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ApiKeys
                .Include(a => a.Scopes)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<ApiKey?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
        {
            return await _context.ApiKeys
                .Include(a => a.Scopes)
                .FirstOrDefaultAsync(a => a.SecretHash.Value == hash, cancellationToken);
        }

        public async Task<IEnumerable<ApiKey>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ApiKeys
                .Include(a => a.Scopes)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
        {
            await _context.ApiKeys.AddAsync(apiKey, cancellationToken);
        }

        public async Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
        {
            _context.ApiKeys.Update(apiKey);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var apiKey = await GetByIdAsync(id, cancellationToken);
            if (apiKey == null) return;

            _context.ApiKeys.Remove(apiKey);
        }
    }
}
