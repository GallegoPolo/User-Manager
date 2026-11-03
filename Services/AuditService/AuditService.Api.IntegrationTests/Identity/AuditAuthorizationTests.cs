using System.Net;
using AuditService.Api.IntegrationTests.Fixtures;

namespace AuditService.Api.IntegrationTests.Identity;

public sealed class AuditAuthorizationTests : IClassFixture<AuditApiFactory>
{
    private const string LogsPath = "/api/audit/logs";

    private readonly AuditApiFactory _api;

    public AuditAuthorizationTests(AuditApiFactory api)
    {
        _api = api;
    }

    [Fact]
    public async Task GetLogs_WithoutToken_Returns401()
    {
        // Act
        var response = await _api.CreateClient().GetAsync(LogsPath);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_AsPersonWithoutRole_Returns403()
    {
        // Act
        var response = await _api.ClientWithRoles().GetAsync(LogsPath);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("viewer")]
    [InlineData("operator")]
    public async Task GetLogs_AsViewerOrOperator_Returns200(string role)
    {
        // Act
        var response = await _api.ClientWithRoles(role).GetAsync(LogsPath);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthLive_WithoutToken_IsNotUnauthorized()
    {
        // Act
        var response = await _api.CreateClient().GetAsync("/health/live");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
