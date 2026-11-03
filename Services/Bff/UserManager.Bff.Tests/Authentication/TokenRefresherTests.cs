using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using UserManager.Bff.Authentication;

namespace UserManager.Bff.Tests.Authentication;

public sealed class TokenRefresherTests
{
    private const string TokenEndpoint = "https://keycloak.test/token";
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTokenEndpoint _endpoint = new();

    [Fact]
    public async Task Validate_WhenTokenFarFromExpiry_DoesNotCallTokenEndpoint()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddMinutes(4));

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.Empty(_endpoint.Requests);
        Assert.False(context.ShouldRenew);
        Assert.NotNull(context.Principal);
    }

    [Fact]
    public async Task Validate_WhenTokenNearExpiry_StoresNewTokensAndRenews()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(30));
        _endpoint.Respond(HttpStatusCode.OK, """{"access_token":"new-access","refresh_token":"new-refresh","expires_in":300}""");

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        var body = Assert.Single(_endpoint.Requests);
        Assert.Contains("grant_type=refresh_token", body);
        Assert.Contains("refresh_token=old-refresh", body);
        Assert.Contains("client_id=user-manager-bff", body);
        Assert.Equal("new-access", context.Properties.GetTokenValue("access_token"));
        Assert.Equal("new-refresh", context.Properties.GetTokenValue("refresh_token"));
        Assert.Equal(Now.AddSeconds(300), DateTimeOffset.Parse(context.Properties.GetTokenValue("expires_at")!, CultureInfo.InvariantCulture));
        Assert.True(context.ShouldRenew);
    }

    [Fact]
    public async Task Validate_WhenRefreshFails_RejectsPrincipal()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(-10));
        _endpoint.Respond(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task Validate_WhenNearExpiryWithoutRefreshToken_RejectsWithoutCallingEndpoint()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(10), refreshToken: null);

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.Null(context.Principal);
        Assert.Empty(_endpoint.Requests);
    }

    [Fact]
    public async Task Validate_WhenKeycloakUnreachableAndTokenStillValid_KeepsSession()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(30));
        _endpoint.FailWith(new HttpRequestException("connection refused"));

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.NotNull(context.Principal);
        Assert.False(context.ShouldRenew);
        Assert.Equal("old-access", context.Properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task Validate_WhenKeycloakUnreachableAndTokenExpired_RejectsPrincipal()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(-5));
        _endpoint.FailWith(new HttpRequestException("connection refused"));

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task Validate_WhenKeycloakReturns503AndTokenStillValid_KeepsSession()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(30));
        _endpoint.Respond(HttpStatusCode.ServiceUnavailable, "{}");

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.NotNull(context.Principal);
    }

    [Fact]
    public async Task Validate_WhenResponseHasNoRefreshToken_KeepsOldRefreshToken()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(30));
        _endpoint.Respond(HttpStatusCode.OK, """{"access_token":"new-access","expires_in":300}""");

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.Equal("new-access", context.Properties.GetTokenValue("access_token"));
        Assert.Equal("old-refresh", context.Properties.GetTokenValue("refresh_token"));
        Assert.True(context.ShouldRenew);
    }

    [Fact]
    public async Task Validate_WhenResponseHasNoAccessToken_RejectsPrincipal()
    {
        // Arrange
        var context = ContextWithTokens(expiresAt: Now.AddSeconds(30));
        _endpoint.Respond(HttpStatusCode.OK, """{"refresh_token":"new-refresh"}""");

        // Act
        await Refresher().ValidateAsync(context);

        // Assert
        Assert.Null(context.Principal);
    }

    private TokenRefresher Refresher()
    {
        var oidc = new OpenIdConnectOptions { ClientId = "user-manager-bff", ClientSecret = "secret" };
        var configuration = new OpenIdConnectConfiguration { TokenEndpoint = TokenEndpoint };
        oidc.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);

        return new TokenRefresher(new HttpClient(_endpoint), new FixedOptionsMonitor(oidc), new FixedTimeProvider(Now));
    }

    private static CookieValidatePrincipalContext ContextWithTokens(DateTimeOffset expiresAt, string? refreshToken = "old-refresh")
    {
        var tokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token", Value = "old-access" },
            new() { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) }
        };

        if (refreshToken is not null)
            tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });

        var properties = new AuthenticationProperties();
        properties.StoreTokens(tokens);

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "kc-vera")], "test"));
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));

        return new CookieValidatePrincipalContext(new DefaultHttpContext(), scheme, new CookieAuthenticationOptions(), new AuthenticationTicket(principal, properties, scheme.Name));
    }

    private sealed class FakeTokenEndpoint : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.InternalServerError;
        private string _body = "{}";
        private Exception? _failure;

        public List<string> Requests { get; } = [];

        public void Respond(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public void FailWith(Exception failure) => _failure = failure;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(TokenEndpoint, request.RequestUri!.ToString());
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));

            if (_failure is not null)
                throw _failure;

            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class FixedOptionsMonitor(OpenIdConnectOptions options) : IOptionsMonitor<OpenIdConnectOptions>
    {
        public OpenIdConnectOptions CurrentValue => options;
        public OpenIdConnectOptions Get(string? name) => options;
        public IDisposable? OnChange(Action<OpenIdConnectOptions, string?> listener) => null;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
