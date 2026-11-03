using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserManager.Api.IntegrationTests.Fixtures;

namespace UserManager.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class AuthorizationMatrixTests
{
    private readonly UserManagerApiFactory _api;

    public AuthorizationMatrixTests(UserManagerApiFactory api)
    {
        _api = api;
    }

    [Theory]
    [InlineData("GET", "/api/me")]
    [InlineData("GET", "/api/me/contacts")]
    [InlineData("GET", "/api/users")]
    public async Task Request_WithoutToken_Returns401(string method, string path)
    {
        // Act
        var response = await _api.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsPersonWithoutRole_Returns403()
    {
        // Act
        var response = await _api.ClientFor(TestPerson.New("Ana")).GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("viewer")]
    [InlineData("operator")]
    public async Task GetUsers_AsViewerOrOperator_Returns200(string role)
    {
        // Act
        var response = await _api.ClientFor(TestPerson.New("Vera", role)).GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_AsViewer_ReturnsTheUser()
    {
        // Arrange
        var targetId = await ProvisionAsync(TestPerson.New("Bia"));

        // Act
        var response = await _api.ClientFor(TestPerson.New("Vera", "viewer")).GetAsync($"/api/users/{targetId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(targetId, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetUserById_WhenMissing_Returns404()
    {
        // Act
        var response = await _api.ClientFor(TestPerson.New("Vera", "viewer")).GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RenameUser_AsViewer_Returns403()
    {
        // Arrange
        var targetId = await ProvisionAsync(TestPerson.New("Bia"));

        // Act
        var response = await _api.ClientFor(TestPerson.New("Vera", "viewer")).PutAsJsonAsync($"/api/users/{targetId}", new { name = "Outra" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RenameUser_AsOperator_Returns204AndRenames()
    {
        // Arrange
        var targetId = await ProvisionAsync(TestPerson.New("Bia"));

        // Act
        var response = await _api.ClientFor(TestPerson.New("Otto", "operator")).PutAsJsonAsync($"/api/users/{targetId}", new { name = "Bia Lima" });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var context = _api.CreateDbContext();
        Assert.Equal("Bia Lima", (await context.Users.FindAsync(targetId))!.Name);
    }

    [Fact]
    public async Task RenameUser_AsOperatorWhenMissing_Returns404()
    {
        // Act
        var response = await _api.ClientFor(TestPerson.New("Otto", "operator")).PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", new { name = "X" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_AsViewer_Returns403()
    {
        // Arrange
        var targetId = await ProvisionAsync(TestPerson.New("Bia"));

        // Act
        var response = await _api.ClientFor(TestPerson.New("Vera", "viewer")).DeleteAsync($"/api/users/{targetId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_AsOperator_Returns204AndDeletes()
    {
        // Arrange
        var targetId = await ProvisionAsync(TestPerson.New("Bia"));

        // Act
        var response = await _api.ClientFor(TestPerson.New("Otto", "operator")).DeleteAsync($"/api/users/{targetId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var context = _api.CreateDbContext();
        Assert.Null(await context.Users.FindAsync(targetId));
    }

    [Fact]
    public async Task CreateUser_RouteNoLongerExists_Returns405()
    {
        // Act
        var response = await _api.ClientFor(TestPerson.New("Otto", "operator")).PostAsJsonAsync("/api/users", new { name = "X", email = "x@example.com" });

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task ContactsByUserIdInUrl_RouteNoLongerExists_Returns404()
    {
        // Arrange
        var targetId = await ProvisionAsync(TestPerson.New("Bia"));

        // Act
        var response = await _api.ClientFor(TestPerson.New("Otto", "operator")).GetAsync($"/api/users/{targetId}/contacts");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> ProvisionAsync(TestPerson person)
    {
        var response = await _api.ClientFor(person).GetAsync("/api/me");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}
