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
