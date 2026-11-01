using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Infrastructure.IntegrationTests.Fixtures;
using UserManager.Infrastructure.Messaging;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.IntegrationTests.Messaging;

public sealed class AuditEventOutboxUnroutableTests : IClassFixture<PostgresFixture>, IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private const string AuditEventExchange = "Audit.Contracts:AuditEventOccurred";
    private static readonly TimeSpan DeliveryAttemptWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly PostgresFixture _postgres;
    private readonly RabbitMqFixture _rabbitMq;
    private ServiceProvider _services = null!;

    public AuditEventOutboxUnroutableTests(PostgresFixture postgres, RabbitMqFixture rabbitMq)
    {
        _postgres = postgres;
        _rabbitMq = rabbitMq;
    }

    public async Task InitializeAsync()
    {
        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<UserManagerDbContext>(options => options.UseNpgsql(_postgres.ConnectionString))
            .AddAuditMessaging(_rabbitMq.CreateSettings())
            .BuildServiceProvider();

        foreach (var hostedService in _services.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        await _services.GetRequiredService<IBusControl>().WaitForHealthStatus(BusHealthStatus.Healthy, WaitTimeout);
    }

    public async Task DisposeAsync()
    {
        foreach (var hostedService in _services.GetServices<IHostedService>().Reverse())
            await hostedService.StopAsync(CancellationToken.None);

        await _services.DisposeAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenNoQueueIsBound_KeepsMessageInOutboxUntilAQueueIsBound()
    {
        // Arrange
        var auditEvent = new AuditEvent(Guid.NewGuid(),
                                        DateTime.UtcNow,
                                        "user.created",
                                        Guid.NewGuid().ToString(),
                                        "User",
                                        new UserCreatedAuditPayload("Ana", "ana@example.com"));

        // Act
        await using (var scope = _services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAuditEventPublisher>().PublishAsync(auditEvent, CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<UserManagerDbContext>().SaveChangesAsync();
        }

        await Task.Delay(DeliveryAttemptWindow);
        var keptWhileUnroutable = await IsStoredInOutboxAsync(auditEvent.EventId);
        var queueName = await BindQueueAsync();

        // Assert
        Assert.True(keptWhileUnroutable);
        await WaitForMessageAsync(queueName);
    }

    private async Task<bool> IsStoredInOutboxAsync(Guid eventId)
    {
        await using var context = _postgres.CreateContext();
        return await context.Set<OutboxMessage>().AnyAsync(m => m.MessageId == eventId);
    }

    private async Task<string> BindQueueAsync()
    {
        var queueName = $"late-binding-{Guid.NewGuid():N}";
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queueName, AuditEventExchange, routingKey: string.Empty);

        return queueName;
    }

    private async Task WaitForMessageAsync(string queueName)
    {
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var deadline = DateTime.UtcNow + WaitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await channel.BasicGetAsync(queueName, autoAck: true) is not null)
                return;

            await Task.Delay(PollInterval);
        }

        throw new TimeoutException($"Tempo esgotado aguardando a entrega na fila {queueName}.");
    }
}
