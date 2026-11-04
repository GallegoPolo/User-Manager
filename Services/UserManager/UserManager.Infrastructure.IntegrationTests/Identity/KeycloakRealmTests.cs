using System.Text.Json;
using UserManager.Infrastructure.IntegrationTests.Fixtures;

namespace UserManager.Infrastructure.IntegrationTests.Identity;

public sealed class KeycloakRealmTests : IClassFixture<KeycloakFixture>
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
        Assert.Equal("first broker login por senha", provider.GetProperty("firstBrokerLoginFlowAlias").GetString());
        Assert.Equal("${GITHUB_CLIENT_ID:nao-configurado}", config.GetProperty("clientId").GetString());
        Assert.Equal("${GITHUB_CLIENT_SECRET:nao-configurado}", config.GetProperty("clientSecret").GetString());
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
    public async Task BrokerFlow_LinksOnlyWithPassword()
    {
        // Act
        var authenticators = await _keycloak.GetAuthenticatorsOfFlowAsync("first broker login por senha");

        // Assert
        Assert.Contains("idp-username-password-form", authenticators);
        Assert.Contains("auth-otp-form", authenticators);
        Assert.DoesNotContain("idp-email-verification", authenticators);
        Assert.DoesNotContain("idp-auto-link", authenticators);
    }

    private static JsonElement ProviderInRealmFile(string alias)
    {
        var realmFile = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Identity", "user-manager-realm.json")));

        return realmFile.RootElement.GetProperty("identityProviders").EnumerateArray()
            .Single(provider => provider.GetProperty("alias").GetString() == alias);
    }

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
