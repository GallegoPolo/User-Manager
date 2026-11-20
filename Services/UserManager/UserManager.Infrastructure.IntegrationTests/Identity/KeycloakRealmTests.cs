using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using UserManager.Infrastructure.IntegrationTests.Fixtures;

namespace UserManager.Infrastructure.IntegrationTests.Identity;

public sealed partial class KeycloakRealmTests : IClassFixture<KeycloakFixture>
{
    private readonly KeycloakFixture _keycloak;

    public KeycloakRealmTests(KeycloakFixture keycloak)
    {
        _keycloak = keycloak;
    }

    [Fact]
    public async Task AccessToken_ForViewer_HasViewerRoleOnly()
    {
        // Act
        var token = await _keycloak.GetAccessTokenPayloadAsync("vera", "vera123");

        // Assert
        var roles = RolesOf(token);
        Assert.Contains("viewer", roles);
        Assert.DoesNotContain("operator", roles);
    }

    [Fact]
    public async Task AccessToken_ForOperator_HasOperatorRole()
    {
        // Act
        var token = await _keycloak.GetAccessTokenPayloadAsync("otto", "otto123");

        // Assert
        Assert.Contains("operator", RolesOf(token));
    }

    [Fact]
    public async Task AccessToken_ForPersonWithoutRole_HasNeitherViewerNorOperator()
    {
        // Act
        var token = await _keycloak.GetAccessTokenPayloadAsync("ana", "ana123");

        // Assert
        var roles = RolesOf(token);
        Assert.NotEmpty(roles);
        Assert.DoesNotContain("viewer", roles);
        Assert.DoesNotContain("operator", roles);
    }

    [Fact]
    public async Task AccessToken_HasApiAudiences()
    {
        // Act
        var token = await _keycloak.GetAccessTokenPayloadAsync("vera", "vera123");

        // Assert
        var audiences = StringsOf(token, "aud");
        Assert.Contains("user-manager-api", audiences);
        Assert.Contains("audit-api", audiences);
    }

    [Fact]
    public async Task AccessToken_HasSubEmailAndName()
    {
        // Act
        var token = await _keycloak.GetAccessTokenPayloadAsync("vera", "vera123");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token.GetProperty("sub").GetString()));
        Assert.Equal("vera@user-manager.local", token.GetProperty("email").GetString());
        Assert.Equal("Vera Lima", token.GetProperty("name").GetString());
    }

    [Fact]
    public void Realm_AllowsRegistrationWithEmailVerificationAndPasswordReset()
    {
        // Arrange
        var realm = _keycloak.Realm;

        // Assert
        Assert.True(realm.GetProperty("registrationAllowed").GetBoolean());
        Assert.True(realm.GetProperty("verifyEmail").GetBoolean());
        Assert.True(realm.GetProperty("resetPasswordAllowed").GetBoolean());
        Assert.False(realm.GetProperty("duplicateEmailsAllowed").GetBoolean());
        Assert.Equal("mailpit", realm.GetProperty("smtpServer").GetProperty("host").GetString());
        Assert.Equal("1025", realm.GetProperty("smtpServer").GetProperty("port").GetString());
    }

    [Fact]
    public void BffClient_IsConfidentialAuthorizationCodeWithPkceOnly()
    {
        // Arrange
        var client = _keycloak.BffClientAsImported;

        // Assert
        Assert.False(client.GetProperty("publicClient").GetBoolean());
        Assert.True(client.GetProperty("standardFlowEnabled").GetBoolean());
        Assert.False(client.GetProperty("directAccessGrantsEnabled").GetBoolean());
        Assert.False(client.GetProperty("implicitFlowEnabled").GetBoolean());
        Assert.Equal("S256", client.GetProperty("attributes").GetProperty("pkce.code.challenge.method").GetString());
        Assert.Equal(["http://localhost:5173/signin-oidc"], StringsOf(client, "redirectUris"));
        Assert.Equal("http://localhost:5173/signout-callback-oidc", client.GetProperty("attributes").GetProperty("post.logout.redirect.uris").GetString());
    }

    [Fact]
    public async Task GitHubProvider_WithoutSecrets_IsImportedDisabled()
    {
        // Act
        var provider = await _keycloak.GetIdentityProviderAsync("github");

        // Assert
        Assert.False(provider.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task LoginPage_WithoutSecrets_HasPasswordFormAndNoGitHubButton()
    {
        // Arrange
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);

        // Act
        await browser.OpenLoginPageAsync();

        // Assert
        Assert.Contains("kc-form-login", browser.Html);
        Assert.DoesNotContain("social-github", browser.Html);
    }

    [Fact]
    public void RealmFile_ConfiguresGitHubWithoutLiteralSecretsAndWithoutTrustingItsEmail()
    {
        // Arrange
        var provider = ProviderInRealmFile("github");
        var config = provider.GetProperty("config");

        // Assert
        Assert.Equal("${GITHUB_ENABLED:false}", provider.GetProperty("enabled").GetString());
        Assert.False(provider.GetProperty("trustEmail").GetBoolean());
        Assert.Equal("first broker login por reautenticacao", provider.GetProperty("firstBrokerLoginFlowAlias").GetString());
        Assert.Equal("${GITHUB_CLIENT_ID:nao-configurado}", config.GetProperty("clientId").GetString());
        Assert.Equal("${GITHUB_CLIENT_SECRET:nao-configurado}", config.GetProperty("clientSecret").GetString());
        Assert.Equal("IMPORT", config.GetProperty("syncMode").GetString());
    }

    [Fact]
    public async Task GoogleProvider_WithoutSecrets_IsImportedDisabledAndHiddenFromLogin()
    {
        // Arrange
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);

        // Act
        var provider = await _keycloak.GetIdentityProviderAsync("google");
        await browser.OpenLoginPageAsync();

        // Assert
        Assert.False(provider.GetProperty("enabled").GetBoolean());
        Assert.DoesNotContain("social-google", browser.Html);
    }

    [Fact]
    public void RealmFile_ConfiguresGoogleWithoutLiteralSecretsAndWithoutTrustingItsEmail()
    {
        // Arrange
        var provider = ProviderInRealmFile("google");
        var config = provider.GetProperty("config");

        // Assert
        Assert.Equal("${GOOGLE_ENABLED:false}", provider.GetProperty("enabled").GetString());
        Assert.False(provider.GetProperty("trustEmail").GetBoolean());
        Assert.Equal("first broker login por reautenticacao", provider.GetProperty("firstBrokerLoginFlowAlias").GetString());
        Assert.Equal("${GOOGLE_CLIENT_ID:nao-configurado}", config.GetProperty("clientId").GetString());
        Assert.Equal("${GOOGLE_CLIENT_SECRET:nao-configurado}", config.GetProperty("clientSecret").GetString());
        Assert.Equal("IMPORT", config.GetProperty("syncMode").GetString());
    }

    [Theory]
    [InlineData("browser")]
    [InlineData("registration")]
    [InlineData("reset credentials")]
    [InlineData("direct grant")]
    [InlineData("clients")]
    public async Task Realm_WithCustomBrokerFlow_KeepsDefaultFlow(string alias)
    {
        // Act
        var flows = await _keycloak.GetTopLevelFlowAliasesAsync();

        // Assert
        Assert.Contains(alias, flows);
    }

    [Fact]
    public async Task BrokerFlow_RequiresReauthenticationAndNeverEmail()
    {
        // Act
        var authenticators = await _keycloak.GetAuthenticatorsOfFlowAsync("first broker login por reautenticacao");

        // Assert
        Assert.Contains("idp-username-password-form", authenticators);
        Assert.Contains("auth-otp-form", authenticators);
        Assert.DoesNotContain("idp-email-verification", authenticators);
        Assert.DoesNotContain("idp-auto-link", authenticators);
    }

    [Fact]
    public void RealmFile_UsesUserManagerLoginTheme()
    {
        // Act
        var loginTheme = RealmFile().GetProperty("loginTheme").GetString();

        // Assert
        Assert.Equal("user-manager", loginTheme);
    }

    [Fact]
    public async Task LoginPage_LoadsUserManagerStylesheetAndItIsServed()
    {
        // Arrange
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);
        await browser.OpenLoginPageAsync();

        // Act
        var stylesheet = UserManagerStylesheetHref().Match(browser.Html);
        var (status, css) = await browser.DownloadAsync(WebUtility.HtmlDecode(stylesheet.Groups[1].Value));

        // Assert
        Assert.True(stylesheet.Success, "a tela de login não carrega o user-manager.css");
        Assert.Contains("styles.css", browser.Html);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("#1c1410", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrationPage_UsesUserManagerTheme()
    {
        // Arrange
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);
        await browser.OpenLoginPageAsync();

        // Act
        await browser.ClickRegisterAsync();

        // Assert
        Assert.Contains("kc-register-form", browser.Html);
        Assert.Matches(UserManagerStylesheetHref(), browser.Html);
    }

    [Fact]
    public async Task ResetPasswordSentPage_ShowsInlineAlertThatThemeRestyles()
    {
        // Arrange
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);
        await browser.OpenLoginPageAsync();
        await browser.ClickForgotPasswordAsync();

        // Act
        await browser.SubmitFirstFormAsync(("username", "ana"));
        var css = File.ReadAllText(Path.Combine(KeycloakFixture.ThemeDirectory, "login", "resources", "css", "user-manager.css"));

        // Assert
        Assert.Contains("pf-m-inline", browser.Html);
        Assert.Contains(".pf-v5-c-alert.pf-m-inline", css);
        Assert.Contains(".pf-v5-c-alert .kc-feedback-text", css);
    }

    private static JsonElement RealmFile() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Identity", "user-manager-realm.json"))).RootElement;

    private static JsonElement ProviderInRealmFile(string alias) =>
        RealmFile().GetProperty("identityProviders").EnumerateArray()
            .Single(provider => provider.GetProperty("alias").GetString() == alias);

    [GeneratedRegex(@"href=""([^""]*/login/user-manager/css/user-manager\.css[^""]*)""")]
    private static partial Regex UserManagerStylesheetHref();

    private static IReadOnlyList<string> RolesOf(JsonElement token) => StringsOf(token, "roles");

    private static IReadOnlyList<string> StringsOf(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return [];

        return value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.GetString()!).ToList()
            : [value.GetString()!];
    }
}
