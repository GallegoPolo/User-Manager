using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace UserManager.Bff.Sessions;

public sealed class RedisTicketStore : ITicketStore
{
    public const string KeyPrefix = "bff:session:";

    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromHours(8);

    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public RedisTicketStore(IDistributedCache cache, IDataProtectionProvider dataProtection, TimeProvider timeProvider)
    {
        _cache = cache;
        _protector = dataProtection.CreateProtector("UserManager.Bff.Sessions");
        _timeProvider = timeProvider;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString("N");
        await RenewAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var protectedTicket = _protector.Protect(TicketSerializer.Default.Serialize(ticket));
        var options = new DistributedCacheEntryOptions { AbsoluteExpiration = ticket.Properties.ExpiresUtc ?? _timeProvider.GetUtcNow().Add(FallbackLifetime) };

        return _cache.SetAsync(KeyPrefix + key, protectedTicket, options);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var protectedTicket = await _cache.GetAsync(KeyPrefix + key);

        return protectedTicket is null ? null : TicketSerializer.Default.Deserialize(_protector.Unprotect(protectedTicket));
    }

    public Task RemoveAsync(string key) => _cache.RemoveAsync(KeyPrefix + key);
}
