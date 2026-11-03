using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UserManager.Bff.Tests.Fixtures;

public sealed class TestSessionHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationSignOutHandler
{
    public const string SchemeName = "TestSession";
    public const string UserHeader = "X-Test-User";
    public const string RolesHeader = "X-Test-Roles";
    public const string AccessToken = "test-access-token";
    public const string Sid = "sid-123";

    public TestSessionHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    public Task SignOutAsync(AuthenticationProperties? properties) => Task.CompletedTask;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var sub))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new("sub", sub!),
            new("name", "Vera Lima"),
            new("email", "vera@user-manager.local"),
            new("sid", Sid)
        };
        claims.AddRange(Request.Headers[RolesHeader].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(role => new Claim("roles", role)));

        var properties = new AuthenticationProperties();
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = AccessToken }]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName, "name", "roles"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, SchemeName)));
    }
}
