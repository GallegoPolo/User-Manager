using System.Web;
using UserManager.Infrastructure.IntegrationTests.Fixtures;

namespace UserManager.Infrastructure.IntegrationTests.Identity;

[Collection(nameof(KeycloakBrokerCollection))]
public sealed class GoogleBrokerTests
{
    private readonly KeycloakBrokerFixture _keycloak;

    public GoogleBrokerTests(KeycloakBrokerFixture keycloak)
    {
        _keycloak = keycloak;
    }

    [Fact]
    public async Task GoogleButton_RedirectsToGoogleWithConfiguredClientAndEmailScope()
    {
        // Arrange
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);

        // Act
        var authorize = await browser.ClickProviderButtonAsync("google");

        // Assert
        var query = HttpUtility.ParseQueryString(authorize.Query);
        Assert.Equal("accounts.google.com", authorize.Host);
        Assert.Equal(KeycloakBrokerFixture.GoogleClientId, query["client_id"]);
        Assert.Contains("email", query["scope"]!.Split(' '));
        Assert.EndsWith("/realms/user-manager/broker/google/endpoint", query["redirect_uri"]);
    }
}
