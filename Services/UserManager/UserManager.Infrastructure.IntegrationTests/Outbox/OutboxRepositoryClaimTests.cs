using Microsoft.EntityFrameworkCore;
using UserManager.Infrastructure.Outbox;

namespace UserManager.Infrastructure.IntegrationTests.Outbox;

public sealed class OutboxRepositoryClaimTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private const string EmptyPayload = "{}";
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BlockingTolerance = TimeSpan.FromSeconds(5);
    private static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly PostgresFixture _postgres;

    public OutboxRepositoryClaimTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        await using var context = _postgres.CreateContext();
        await context.Database.ExecuteSqlAsync($"""DELETE FROM "OutboxMessages" """);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ClaimPendingAsync_WhenRowsAreLockedByUncommittedClaim_SkipsThemWithoutWaiting()
    {
        // Arrange
        var oldest = await InsertMessageAsync(BaseTime);
        var middle = await InsertMessageAsync(BaseTime.AddSeconds(1));
        var newest = await InsertMessageAsync(BaseTime.AddSeconds(2));

        await using var firstWorker = _postgres.CreateContext();
        await using var uncommittedClaim = await firstWorker.Database.BeginTransactionAsync();
        var heldByFirstWorker = await new OutboxRepository(firstWorker).ClaimPendingAsync(2, Lease, CancellationToken.None);

        await using var secondWorker = _postgres.CreateContext();
        using var timeout = new CancellationTokenSource(BlockingTolerance);

        // Act
        var claimedBySecondWorker = await new OutboxRepository(secondWorker).ClaimPendingAsync(2, Lease, timeout.Token);

        // Assert
        Assert.Equal([oldest, middle], heldByFirstWorker.Select(m => m.Id));
        Assert.Equal(newest, Assert.Single(claimedBySecondWorker).Id);
    }

    [Fact]
    public async Task ClaimPendingAsync_WhenLeaseIsActive_DoesNotClaimAgain()
    {
        // Arrange
        await InsertMessageAsync(BaseTime);
        await ClaimAsync(Lease);

        // Act
        var claimedAgain = await ClaimAsync(Lease);

        // Assert
        Assert.Empty(claimedAgain);
    }

    [Fact]
    public async Task ClaimPendingAsync_WhenLeaseExpired_ClaimsAgain()
    {
        // Arrange
        var messageId = await InsertMessageAsync(BaseTime);
        await ClaimAsync(TimeSpan.FromSeconds(1));
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Act
        var claimedAgain = await ClaimAsync(Lease);

        // Assert
        Assert.Equal(messageId, Assert.Single(claimedAgain).Id);
    }

    [Fact]
    public async Task ClaimPendingAsync_ReturnsOnlyUnprocessedMessagesOldestFirst()
    {
        // Arrange
        await InsertMessageAsync(BaseTime, processedAt: BaseTime.AddMinutes(1));
        var third = await InsertMessageAsync(BaseTime.AddSeconds(3));
        var first = await InsertMessageAsync(BaseTime.AddSeconds(1));
        var second = await InsertMessageAsync(BaseTime.AddSeconds(2));

        // Act
        var claimed = await ClaimAsync(Lease);

        // Assert
        Assert.Equal([first, second, third], claimed.Select(m => m.Id));
    }

    private async Task<List<OutboxMessage>> ClaimAsync(TimeSpan lease)
    {
        await using var context = _postgres.CreateContext();
        return await new OutboxRepository(context).ClaimPendingAsync(10, lease, CancellationToken.None);
    }

    private async Task<Guid> InsertMessageAsync(DateTime occurredAt, DateTime? processedAt = null)
    {
        var id = Guid.NewGuid();

        await using var context = _postgres.CreateContext();
        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO "OutboxMessages" ("Id", "EventType", "AggregateId", "AggregateType", "PayloadJson", "OccurredAt", "ProcessedAt")
            VALUES ({id}, 'user.created', {id.ToString()}, 'User', {EmptyPayload}, {occurredAt}, {processedAt})
            """);

        return id;
    }
}
