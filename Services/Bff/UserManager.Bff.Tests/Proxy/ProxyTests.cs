using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using UserManager.Bff.Tests.Fixtures;

namespace UserManager.Bff.Tests.Proxy;

public sealed class ProxyTests : IAsyncLifetime
{
    private WebApplication _downstream = null!;
    private BffFactory _bff = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _downstream = builder.Build();
        _downstream.Map("/{**path}", (HttpRequest request) => Results.Ok(new
        {
            path = request.Path.Value,
            authorization = request.Headers.Authorization.ToString(),
            cookie = request.Headers.Cookie.ToString(),
            csrf = request.Headers["X-CSRF"].ToString()
        }));
        await _downstream.StartAsync();

        var address = _downstream.Urls.Single();
        _bff = new BffFactory()
            .WithSetting("ReverseProxy:Clusters:usermanager:Destinations:primary:Address", address)
            .WithSetting("ReverseProxy:Clusters:audit:Destinations:primary:Address", address);
    }

    public async Task DisposeAsync()
    {
        await _bff.DisposeAsync();
        await _downstream.DisposeAsync();
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/me/contacts")]
    [InlineData("/api/users")]
    [InlineData("/api/audit/logs")]
    public async Task Api_WithSession_ForwardsBearerTokenAndNoCookie(string path)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(TestSessionHandler.UserHeader, "kc-vera");
        request.Headers.Add("X-CSRF", "1");
        request.Headers.Add("Cookie", "__Host-user-manager=session-id; other=1");

        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(path, echo.GetProperty("path").GetString());
        Assert.Equal($"Bearer {TestSessionHandler.AccessToken}", echo.GetProperty("authorization").GetString());
        Assert.Equal(string.Empty, echo.GetProperty("cookie").GetString());
        Assert.Equal(string.Empty, echo.GetProperty("csrf").GetString());
    }

    [Fact]
    public async Task Api_WithoutSession_Returns401()
    {
        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/users") { Headers = { { "X-CSRF", "1" } } });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_WithoutCsrfHeader_Returns400()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Add(TestSessionHandler.UserHeader, "kc-vera");

        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
