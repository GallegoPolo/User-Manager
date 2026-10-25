using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UserManager.Infrastructure.Outbox;

namespace UserManager.Infrastructure.IntegrationTests.Outbox;

public sealed class OutboxRepositoryTraceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public OutboxRepositoryTraceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Add_WithActiveW3CTrace_StoresTraceParent()
    {
        // Arrange
        using var request = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();

        // Act
        var aggregateId = await AddMessageAsync();

        // Assert
        Assert.Equal(request.Id, await ReadTraceParentAsync(aggregateId));
    }

    [Fact]
    public async Task Add_WithoutActiveTrace_StoresNoTraceParent()
    {
        // Act
        var aggregateId = await AddMessageAsync();

        // Assert
        Assert.Null(await ReadTraceParentAsync(aggregateId));
    }

    [Fact]
    public async Task Add_WithHierarchicalTrace_StoresNoTraceParent()
    {
        // Arrange
        using var request = new Activity("request").SetIdFormat(ActivityIdFormat.Hierarchical).Start();

        // Act
        var aggregateId = await AddMessageAsync();

        // Assert
        Assert.Null(await ReadTraceParentAsync(aggregateId));
    }

    private async Task<string> AddMessageAsync()
    {
        var aggregateId = Guid.NewGuid().ToString();

        await using var context = _postgres.CreateContext();
        new OutboxRepository(context).Add("user.created", aggregateId, "User", "{}");
        await context.SaveChangesAsync();

        return aggregateId;
    }

    private async Task<string?> ReadTraceParentAsync(string aggregateId)
    {
        await using var context = _postgres.CreateContext();

        return await context.OutboxMessages
            .Where(m => m.AggregateId == aggregateId)
            .Select(m => m.TraceParent)
            .SingleAsync();
    }
}
