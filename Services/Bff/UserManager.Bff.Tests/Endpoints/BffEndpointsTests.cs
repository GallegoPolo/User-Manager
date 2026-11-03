using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserManager.Bff.Endpoints;
using UserManager.Bff.Tests.Fixtures;

namespace UserManager.Bff.Tests.Endpoints;

public sealed class BffEndpointsTests : IClassFixture<BffFactory>
{
    private readonly BffFactory _bff;

    public BffEndpointsTests(BffFactory bff)
    {
        _bff = bff;
    }

    [Fact]
    public async Task User_WithoutCsrfHeader_Returns400()
    {
        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(SessionRequest("/bff/user", csrf: false));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_WithoutSession_Returns401InsteadOfRedirect()
    {
        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, "/bff/user") { Headers = { { "X-CSRF", "1" } } });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_WithSession_ReturnsClaimsRolesAndLogoutUrlWithSid()
    {
        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(SessionRequest("/bff/user", roles: "viewer"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("kc-vera", body.GetProperty("sub").GetString());
        Assert.Equal("Vera Lima", body.GetProperty("name").GetString());
        Assert.Equal("vera@user-manager.local", body.GetProperty("email").GetString());
        Assert.Equal(["viewer"], body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        Assert.Equal($"/bff/logout?sid={TestSessionHandler.Sid}", body.GetProperty("logoutUrl").GetString());
    }

    [Fact]
    public async Task Login_RedirectsToKeycloakAuthorizeWithPkce()
    {
        // Act
        var response = await _bff.CreateBrowserClient().GetAsync("/bff/login?returnUrl=/users");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith(BffFactory.AuthorizationEndpoint, location);
        Assert.Contains("code_challenge_method=S256", location);
        Assert.Contains("response_type=code", location);
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("/users", "/users")]
    [InlineData("https://evil.example/steal", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("/\t/evil.example", "/")]
    [InlineData("/\n/evil.example", "/")]
    [InlineData("/\t\\evil.example", "/")]
    [InlineData("/users\r\nSet-Cookie: x=1", "/")]
    public void SafeReturnUrl_KeepsOnlyLocalPaths(string? returnUrl, string expected)
    {
        // Act
        var safe = BffEndpoints.SafeReturnUrl(returnUrl);

        // Assert
        Assert.Equal(expected, safe);
    }

    [Fact]
    public async Task Logout_WithWrongSid_Returns400()
    {
        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(SessionRequest("/bff/logout?sid=other", csrf: false));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutSession_RedirectsHome()
    {
        // Act
        var response = await _bff.CreateBrowserClient().GetAsync("/bff/logout?sid=whatever");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Logout_WithRightSid_RedirectsToKeycloakEndSession()
    {
        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(SessionRequest($"/bff/logout?sid={TestSessionHandler.Sid}", csrf: false));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(BffFactory.EndSessionEndpoint, response.Headers.Location!.ToString());
    }

    private static HttpRequestMessage SessionRequest(string path, string roles = "", bool csrf = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(TestSessionHandler.UserHeader, "kc-vera");
        request.Headers.Add(TestSessionHandler.RolesHeader, roles);

        if (csrf)
            request.Headers.Add("X-CSRF", "1");

        return request;
    }
}
