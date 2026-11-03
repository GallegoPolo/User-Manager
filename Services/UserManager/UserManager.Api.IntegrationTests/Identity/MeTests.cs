using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UserManager.Api.IntegrationTests.Fixtures;

namespace UserManager.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class MeTests
{
    private readonly UserManagerApiFactory _api;

    public MeTests(UserManagerApiFactory api)
    {
        _api = api;
    }

    [Fact]
    public async Task GetMe_FirstCall_ProvisionsUserFromToken()
    {
        // Arrange
        var ana = TestPerson.New("Ana");

        // Act
        var response = await _api.ClientFor(ana).GetAsync("/api/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ana", body.GetProperty("name").GetString());
        Assert.Equal(ana.Email, body.GetProperty("email").GetString());
        await using var context = _api.CreateDbContext();
        Assert.Equal(1, await context.Users.CountAsync(u => u.IdentityProviderId == ana.Sub));
    }

    [Fact]
    public async Task GetMe_SecondCall_ReturnsTheSameUser()
    {
        // Arrange
        var client = _api.ClientFor(TestPerson.New("Ana"));
        var first = await (await client.GetAsync("/api/me")).Content.ReadFromJsonAsync<JsonElement>();

        // Act
        var second = await (await client.GetAsync("/api/me")).Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetMe_WithTokenWithoutEmail_Returns403AndCreatesNothing()
    {
        // Arrange
        var noEmail = TestPerson.New("Ana") with { Email = null };

        // Act
        var response = await _api.ClientFor(noEmail).GetAsync("/api/me");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var context = _api.CreateDbContext();
        Assert.Equal(0, await context.Users.CountAsync(u => u.IdentityProviderId == noEmail.Sub));
    }

    [Fact]
    public async Task GetMe_AfterEmailChangedInIdentityProvider_SyncsEmail()
    {
        // Arrange
        var ana = TestPerson.New("Ana");
        await _api.ClientFor(ana).GetAsync("/api/me");
        var renamedEmail = ana with { Email = $"novo-{ana.Email}" };

        // Act
        var body = await (await _api.ClientFor(renamedEmail).GetAsync("/api/me")).Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(renamedEmail.Email, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task RenameMe_ChangesDisplayNameAndLoginKeepsIt()
    {
        // Arrange
        var client = _api.ClientFor(TestPerson.New("Ana"));
        await client.GetAsync("/api/me");

        // Act
        var rename = await client.PutAsJsonAsync("/api/me", new { name = "Ana do App" });
        var body = await (await client.GetAsync("/api/me")).Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, rename.StatusCode);
        Assert.Equal("Ana do App", body.GetProperty("name").GetString());
    }
}
