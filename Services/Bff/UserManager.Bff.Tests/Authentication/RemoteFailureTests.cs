using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using UserManager.Bff.Tests.Fixtures;

namespace UserManager.Bff.Tests.Authentication;

public sealed partial class RemoteFailureTests : IClassFixture<BffFactory>
{
    private const string FailedLoginHome = "/?login=falhou";

    private readonly BffFactory _bff;

    public RemoteFailureTests(BffFactory bff)
    {
        _bff = bff;
    }

    [Fact]
    public async Task Callback_WithoutCorrelationCookie_RedirectsHomeInsteadOfThrowing()
    {
        // Arrange
        var browser = _bff.CreateBrowserClient();
        var login = await browser.GetAsync("/bff/login?returnUrl=/contacts");
        var state = StateParameter().Match(login.Headers.Location!.ToString()).Groups[1].Value;

        // Act
        var callback = await _bff.CreateBrowserClient().GetAsync($"/signin-oidc?code=any&state={state}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal(FailedLoginHome, callback.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Callback_WithUnreadableState_RedirectsHomeInsteadOfThrowing()
    {
        // Act
        var callback = await _bff.CreateBrowserClient().GetAsync("/signin-oidc?code=any&state=forged");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal(FailedLoginHome, callback.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Callback_WhenKeycloakTokenEndpointFails_RedirectsHomeAsUnavailable()
    {
        // Arrange
        var bff = _bff.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Configuration!.TokenEndpoint = "https://keycloak.test/token";
                options.Backchannel = new HttpClient(new FailingTokenEndpoint());
            })));
        var login = await bff.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
            .GetAsync("/bff/login?returnUrl=/contacts");
        var state = StateParameter().Match(login.Headers.Location!.ToString()).Groups[1].Value;
        var cookies = string.Join("; ", login.Headers.GetValues("Set-Cookie").Select(cookie => cookie.Split(';')[0]));
        var callback = new HttpRequestMessage(HttpMethod.Get, $"/signin-oidc?code=any&state={state}");
        callback.Headers.Add("Cookie", cookies);

        // Act
        var response = await bff.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }).SendAsync(callback);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/?login=indisponivel", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Callback_WhenPersonCancelsAtKeycloak_RedirectsHomeAsFailedLogin()
    {
        // Arrange
        var login = await _bff.CreateBrowserClient().GetAsync("/bff/login?returnUrl=/contacts");
        var state = StateParameter().Match(login.Headers.Location!.ToString()).Groups[1].Value;
        var cookies = string.Join("; ", login.Headers.GetValues("Set-Cookie").Select(cookie => cookie.Split(';')[0]));
        var callback = new HttpRequestMessage(HttpMethod.Get, $"/signin-oidc?error=access_denied&state={state}");
        callback.Headers.Add("Cookie", cookies);

        // Act
        var response = await _bff.CreateBrowserClient().SendAsync(callback);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(FailedLoginHome, response.Headers.Location!.ToString());
    }

    private sealed class FailingTokenEndpoint : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }

    [GeneratedRegex("[?&]state=([^&]+)")]
    private static partial Regex StateParameter();
}
