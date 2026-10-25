using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using UserManager.Application.Events.Interfaces;
using UserManager.Infrastructure.IntegrationTests.TestDoubles;
using UserManager.Infrastructure.Outbox;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.IntegrationTests.Outbox;

public sealed class OutboxProcessorTracingTests : IClassFixture<PostgresFixture>
{
    private const string StoredTraceId = "0af7651916cd43dd8448eb211c80319c";
    private const string StoredSpanId = "b7ad6b7169203331";
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(15);

    private readonly PostgresFixture _postgres;

    public OutboxProcessorTracingTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Worker_PublishesInsideStoredTrace()
    {
        // Arrange
        using var listener = new ActivitySourceListener(OutboxTelemetry.ActivitySourceName);
        await InsertMessageAsync($"00-{StoredTraceId}-{StoredSpanId}-01");

        var publisher = new TraceRecordingPublisher();
        await using var services = BuildServices(publisher);
        var worker = new OutboxProcessorBackgroundService(services.GetRequiredService<IServiceScopeFactory>(),
                                                          NullLogger<OutboxProcessorBackgroundService>.Instance);

        // Act
        await worker.StartAsync(CancellationToken.None);
        var published = await publisher.FirstPublish.WaitAsync(PublishTimeout);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(StoredTraceId, published.TraceId);
        Assert.Equal(StoredSpanId, published.ParentSpanId);
    }

    private ServiceProvider BuildServices(IAuditEventPublisher publisher)
    {
        return new ServiceCollection()
            .AddDbContext<UserManagerDbContext>(options => options.UseNpgsql(_postgres.ConnectionString))
            .AddScoped<OutboxRepository>()
            .AddSingleton(publisher)
            .BuildServiceProvider();
    }

    private async Task InsertMessageAsync(string traceParent)
    {
        await using var context = _postgres.CreateContext();
        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO "OutboxMessages" ("Id", "EventType", "AggregateId", "AggregateType", "PayloadJson", "OccurredAt", "TraceParent")
            VALUES ({Guid.NewGuid()}, 'user.created', 'tracing-test', 'User', {"{}"}, {DateTime.UtcNow}, {traceParent})
            """);
    }

    private sealed record PublishedTrace(string? TraceId, string? ParentSpanId);

    private sealed class TraceRecordingPublisher : IAuditEventPublisher
    {
        private readonly TaskCompletionSource<PublishedTrace> _firstPublish = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<PublishedTrace> FirstPublish => _firstPublish.Task;

        public Task PublishAsync(Guid eventId,
                                 string eventType,
                                 string aggregateId,
                                 string aggregateType,
                                 string performedBy,
                                 DateTime occurredAt,
                                 string payloadJson,
                                 CancellationToken cancellationToken = default)
        {
            var current = Activity.Current;
            _firstPublish.TrySetResult(new PublishedTrace(current?.TraceId.ToString(), current?.ParentSpanId.ToString()));

            return Task.CompletedTask;
        }
    }
}
