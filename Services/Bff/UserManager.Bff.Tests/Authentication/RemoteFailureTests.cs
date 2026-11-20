using System.Net;
using System.Text.RegularExpressions;
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

    [GeneratedRegex("[?&]state=([^&]+)")]
    private static partial Regex StateParameter();
}
