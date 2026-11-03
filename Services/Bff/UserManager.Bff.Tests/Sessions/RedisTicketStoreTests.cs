using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Testcontainers.Redis;
using UserManager.Bff.Sessions;

namespace UserManager.Bff.Tests.Sessions;

public sealed class RedisTicketStoreTests : IAsyncLifetime
{
    private const string AccessToken = "secret-access-token-value";

    private readonly RedisContainer _redis = new RedisBuilder("redis:7").Build();
    private RedisCache _cache = null!;
    private RedisTicketStore _store = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        _cache = new RedisCache(Options.Create(new RedisCacheOptions { Configuration = _redis.GetConnectionString() }));
        _store = new RedisTicketStore(_cache, new EphemeralDataProtectionProvider(), TimeProvider.System);
    }

    public async Task DisposeAsync()
    {
        _cache.Dispose();
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task Store_ThenRetrieve_ReturnsSameTicketWithTokens()
    {
        // Arrange
        var key = await _store.StoreAsync(Ticket("kc-ana"));

        // Act
        var retrieved = await _store.RetrieveAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("kc-ana", retrieved.Principal.FindFirst("sub")?.Value);
        Assert.Equal(AccessToken, retrieved.Properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task Remove_ThenRetrieve_ReturnsNull()
    {
        // Arrange
        var key = await _store.StoreAsync(Ticket("kc-ana"));

        // Act
        await _store.RemoveAsync(key);

        // Assert
        Assert.Null(await _store.RetrieveAsync(key));
    }

    [Fact]
    public async Task Renew_ReplacesStoredTicket()
    {
        // Arrange
        var key = await _store.StoreAsync(Ticket("kc-ana"));

        // Act
        await _store.RenewAsync(key, Ticket("kc-bia"));

        // Assert
        Assert.Equal("kc-bia", (await _store.RetrieveAsync(key))!.Principal.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task StoredValue_DoesNotContainTokenInPlainText()
    {
        // Arrange
        var key = await _store.StoreAsync(Ticket("kc-ana"));

        // Act
        var raw = await _cache.GetAsync(RedisTicketStore.KeyPrefix + key);

        // Assert
        Assert.NotNull(raw);
        Assert.DoesNotContain(AccessToken, Encoding.UTF8.GetString(raw));
    }

    private static AuthenticationTicket Ticket(string sub)
    {
        var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = AccessToken }]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", sub)], "test"));
        return new AuthenticationTicket(principal, properties, "Cookies");
    }
}
