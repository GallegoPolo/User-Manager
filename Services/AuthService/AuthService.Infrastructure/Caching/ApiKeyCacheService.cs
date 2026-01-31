using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Infrastructure.Configurations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AuthService.Infrastructure.Caching
{
    public class ApiKeyCacheService : IApiKeyCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _cacheDuration;
        private const string KEY_PREFIX = "apikey:prefix:";

        public ApiKeyCacheService(IDistributedCache cache, IOptions<RedisSettings> options)
        {
            _cache = cache;
            var minutes = options.Value.CacheDurationMinutes;
            _cacheDuration = TimeSpan.FromMinutes(minutes);
        }

        public async Task<ApiKey?> GetCachedApiKeyAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{KEY_PREFIX}{prefix}";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
                return null;

            return JsonSerializer.Deserialize<ApiKey>(cachedData);
        }

        public async Task SetCachedApiKeyAsync(string prefix, ApiKey apiKey, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{KEY_PREFIX}{prefix}";
            var serialized = JsonSerializer.Serialize(apiKey);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            };

            await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
        }

        public async Task RemoveCachedApiKeyAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{KEY_PREFIX}{prefix}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }
    }
}
