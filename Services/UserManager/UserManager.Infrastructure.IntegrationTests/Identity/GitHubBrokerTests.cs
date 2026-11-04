using UserManager.Infrastructure.IntegrationTests.Fixtures;

namespace UserManager.Infrastructure.IntegrationTests.Identity;

public sealed class GitHubBrokerTests : IClassFixture<KeycloakBrokerFixture>
{
    private readonly KeycloakBrokerFixture _keycloak;

    public GitHubBrokerTests(KeycloakBrokerFixture keycloak)
    {
        _keycloak = keycloak;
    }

    [Fact]
    public async Task NewPerson_MustConfirmEmailBeforeGettingToken()
    {
        // Arrange
        const string email = "nova@example.com";
        await _keycloak.GitHubAnswersAsAsync(1001, "nova-gh", "Nova Costa", email);
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);

        // Act
        await browser.SignInWithGitHubAsync();

        // Assert
        var user = await _keycloak.FindUserByEmailAsync(email);
        Assert.Null(browser.AuthorizationCode);
        Assert.False(user.GetProperty("emailVerified").GetBoolean());
        Assert.Equal(1, await _keycloak.CountFederatedIdentitiesAsync(user.GetProperty("id").GetString()!));

        // Act
        await browser.OpenLinkAsync(await _keycloak.WaitForActionLinkAsync(email));

        // Assert
        Assert.NotNull(browser.AuthorizationCode);
        var token = await _keycloak.ExchangeCodeAsync(browser.AuthorizationCode!, browser.CodeVerifier);
        Assert.Equal(user.GetProperty("id").GetString(), token.GetProperty("sub").GetString());
        Assert.Equal(email, token.GetProperty("email").GetString());
        Assert.Equal("Nova Costa", token.GetProperty("name").GetString());
    }

    [Fact]
    public async Task SecondLogin_KeepsSameSubWithoutNewConfirmation()
    {
        // Arrange
        const string email = "repete@example.com";
        await _keycloak.GitHubAnswersAsAsync(1002, "repete-gh", "Repete Silva", email);
        using var first = new BrokerBrowser(_keycloak.KeycloakUrl);
        await first.SignInWithGitHubAsync();
        await first.OpenLinkAsync(await _keycloak.WaitForActionLinkAsync(email));
        var firstToken = await _keycloak.ExchangeCodeAsync(first.AuthorizationCode!, first.CodeVerifier);
        using var second = new BrokerBrowser(_keycloak.KeycloakUrl);

        // Act
        await second.SignInWithGitHubAsync();

        // Assert
        Assert.NotNull(second.AuthorizationCode);
        var secondToken = await _keycloak.ExchangeCodeAsync(second.AuthorizationCode!, second.CodeVerifier);
        Assert.Equal(firstToken.GetProperty("sub").GetString(), secondToken.GetProperty("sub").GetString());
        Assert.Equal(1, await _keycloak.CountMailsToAsync(email));
    }

    [Fact]
    public async Task ExistingEmail_IsNotLinkedWithoutProof()
    {
        // Arrange
        const string email = "vera@user-manager.local";
        await _keycloak.GitHubAnswersAsAsync(1003, "falsa-vera", "Falsa Vera", email);
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);

        // Act
        await browser.SignInWithGitHubAsync();

        // Assert
        var vera = await _keycloak.FindUserByEmailAsync(email);
        Assert.Null(browser.AuthorizationCode);
        Assert.Contains("linkAccount", browser.Html);
        Assert.Equal(0, await _keycloak.CountFederatedIdentitiesAsync(vera.GetProperty("id").GetString()!));
    }

    [Fact]
    public async Task ExistingEmail_WithOwnersPassword_KeepsTheExistingSub()
    {
        // Arrange
        const string email = "ana@user-manager.local";
        await _keycloak.GitHubAnswersAsAsync(1004, "ana-gh", "Ana Souza", email);
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);
        var ana = await _keycloak.FindUserByEmailAsync(email);
        var anaId = ana.GetProperty("id").GetString()!;
        await browser.SignInWithGitHubAsync();
        await browser.SubmitFirstFormAsync(("submitAction", "linkAccount"));

        // Act
        await browser.SubmitFirstFormAsync(("username", "ana"), ("password", "ana123"));

        // Assert
        Assert.NotNull(browser.AuthorizationCode);
        var token = await _keycloak.ExchangeCodeAsync(browser.AuthorizationCode!, browser.CodeVerifier);
        Assert.Equal(anaId, token.GetProperty("sub").GetString());
        Assert.Equal(1, await _keycloak.CountFederatedIdentitiesAsync(anaId));
    }

    [Fact]
    public async Task ExistingEmail_WithWrongPassword_IsNotLinked()
    {
        // Arrange
        const string email = "vera@user-manager.local";
        await _keycloak.GitHubAnswersAsAsync(1006, "vera-gh", "Vera Lima", email);
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);
        await browser.SignInWithGitHubAsync();
        await browser.SubmitFirstFormAsync(("submitAction", "linkAccount"));

        // Act
        await browser.SubmitFirstFormAsync(("username", "vera"), ("password", "senha-errada"));

        // Assert
        var vera = await _keycloak.FindUserByEmailAsync(email);
        Assert.Null(browser.AuthorizationCode);
        Assert.Equal(0, await _keycloak.CountFederatedIdentitiesAsync(vera.GetProperty("id").GetString()!));
    }

    [Fact]
    public async Task ExistingUsername_CannotAskTheOwnerToApproveTheLinkByEmail()
    {
        // Arrange
        const string ottoEmail = "otto@user-manager.local";
        await _keycloak.GitHubAnswersAsAsync(1007, "otto", "Falso Otto", "atacante-otto@example.com");
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);
        await browser.SignInWithGitHubAsync();

        // Act
        await browser.SubmitFirstFormAsync(("submitAction", "linkAccount"));

        // Assert
        var otto = await _keycloak.FindUserByEmailAsync(ottoEmail);
        Assert.Null(browser.AuthorizationCode);
        Assert.Contains("name=\"password\"", browser.Html);
        Assert.Equal(0, await _keycloak.CountMailsToAsync(ottoEmail));
        Assert.Equal(0, await _keycloak.CountFederatedIdentitiesAsync(otto.GetProperty("id").GetString()!));
    }

    [Theory]
    [InlineData(1005, "sem-nome", null, "semnome@example.com")]
    [InlineData(1008, "uma-palavra", "Solo", "solo@example.com")]
    public async Task PersonWithIncompleteName_ReviewsProfileAndGetsNameInToken(long gitHubId, string login, string? gitHubName, string email)
    {
        // Arrange
        await _keycloak.GitHubAnswersAsAsync(gitHubId, login, gitHubName, email);
        using var browser = new BrokerBrowser(_keycloak.KeycloakUrl);
        await browser.SignInWithGitHubAsync();

        // Assert
        Assert.Null(browser.AuthorizationCode);
        Assert.Contains("name=\"firstName\"", browser.Html);
        Assert.Contains("name=\"lastName\"", browser.Html);

        // Act
        await browser.SubmitFirstFormAsync(("username", login), ("email", email), ("firstName", "Sem"), ("lastName", "Nome"));
        await browser.OpenLinkAsync(await _keycloak.WaitForActionLinkAsync(email));

        // Assert
        Assert.NotNull(browser.AuthorizationCode);
        var token = await _keycloak.ExchangeCodeAsync(browser.AuthorizationCode!, browser.CodeVerifier);
        Assert.Equal("Sem Nome", token.GetProperty("name").GetString());
    }
}
