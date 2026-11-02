using System.Diagnostics;
using System.Text.Json;
using MassTransit;
using MassTransit.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using UserManager.Application.Events.Interfaces;
using UserManager.Contracts;
using UserManager.Infrastructure.IntegrationTests.Fixtures;
using UserManager.Infrastructure.IntegrationTests.TestDoubles;
using UserManager.Infrastructure.Messaging;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.IntegrationTests.Messaging;

public sealed class IntegrationEventOutboxTests : IClassFixture<PostgresFixture>, IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private const string UserCreatedExchange = "UserManager.Contracts:UserCreated";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly PostgresFixture _postgres;
    private readonly RabbitMqFixture _rabbitMq;
    private readonly string _queueName = $"outbox-test-{Guid.NewGuid():N}";
    private ServiceProvider _services = null!;
    private IConnection _connection = null!;

    public IntegrationEventOutboxTests(PostgresFixture postgres, RabbitMqFixture rabbitMq)
    {
        _postgres = postgres;
        _rabbitMq = rabbitMq;
    }

    public async Task InitializeAsync()
    {
        _connection = await _rabbitMq.CreateConnectionAsync();
        await BindTestQueueAsync();

        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<UserManagerDbContext>(options => options.UseNpgsql(_postgres.ConnectionString))
            .AddIntegrationMessaging(_rabbitMq.CreateSettings())
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
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task PublishAsync_ThenSaveChanges_DeliversMessageToBroker()
    {
        // Arrange
        var userCreated = SampleUserCreated();

        // Act
        await PublishAsync(userCreated, saveChanges: true);

        // Assert
        var delivered = await WaitForDeliveryAsync(userCreated.EventId);
        var message = delivered.Envelope.GetProperty("message");

        Assert.Equal(["urn:message:UserManager.Contracts:UserCreated"], delivered.Envelope.GetProperty("messageType").EnumerateArray().Select(t => t.GetString()));
        Assert.Equal(userCreated.UserId, message.GetProperty("userId").GetGuid());
        Assert.Equal("Ana", message.GetProperty("name").GetString());
        Assert.Equal("ana@example.com", message.GetProperty("email").GetString());
    }

    [Fact]
    public async Task PublishAsync_WithoutSaveChanges_DeliversNothing()
    {
        // Arrange
        var discarded = SampleUserCreated();
        var committed = SampleUserCreated();

        // Act
        await PublishAsync(discarded, saveChanges: false);
        await PublishAsync(committed, saveChanges: true);

        // Assert
        var delivered = await WaitForDeliveryAsync(committed.EventId);
        Assert.DoesNotContain(discarded.EventId, delivered.EventIdsSeen);
    }

    [Fact]
    public async Task PublishAsync_InsideRequestTrace_DeliversTraceParentOfThatTrace()
    {
        // Arrange
        using var listener = new ActivitySourceListener(DiagnosticHeaders.DefaultListenerName);
        var userCreated = SampleUserCreated();
        using var request = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();

        // Act
        await PublishAsync(userCreated, saveChanges: true);
        request.Stop();

        // Assert
        var delivered = await WaitForDeliveryAsync(userCreated.EventId);
        Assert.Contains(request.TraceId.ToString(), delivered.TraceParent);
    }

    private async Task PublishAsync(UserCreated userCreated, bool saveChanges)
    {
        await using var scope = _services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(userCreated, CancellationToken.None);

        if (saveChanges)
            await scope.ServiceProvider.GetRequiredService<UserManagerDbContext>().SaveChangesAsync();
    }

    private static UserCreated SampleUserCreated() => new()
    {
        EventId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        UserId = Guid.NewGuid(),
        Name = "Ana",
        Email = "ana@example.com"
    };

    private async Task BindTestQueueAsync()
    {
        await using var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(UserCreatedExchange, ExchangeType.Fanout, durable: true);
        await channel.QueueDeclareAsync(_queueName, durable: false, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(_queueName, UserCreatedExchange, routingKey: string.Empty);
    }

    private async Task<Delivery> WaitForDeliveryAsync(Guid eventId)
    {
        await using var channel = await _connection.CreateChannelAsync();
        var eventIdsSeen = new List<Guid>();
        var deadline = DateTime.UtcNow + WaitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var result = await channel.BasicGetAsync(_queueName, autoAck: true);
            if (result is null)
            {
                await Task.Delay(PollInterval);
                continue;
            }

            var envelope = JsonDocument.Parse(result.Body).RootElement.Clone();
            var deliveredEventId = envelope.GetProperty("message").GetProperty("eventId").GetGuid();
            eventIdsSeen.Add(deliveredEventId);

            if (deliveredEventId == eventId)
                return new Delivery(envelope, ReadTraceParent(result.BasicProperties), eventIdsSeen);
        }

        throw new TimeoutException($"Tempo esgotado aguardando o evento {eventId} na fila {_queueName}.");
    }

    private static string? ReadTraceParent(IReadOnlyBasicProperties properties)
    {
        return properties.Headers?.TryGetValue(DiagnosticHeaders.ActivityId, out var value) == true && value is byte[] bytes
            ? System.Text.Encoding.UTF8.GetString(bytes)
            : null;
    }

    private sealed record Delivery(JsonElement Envelope, string? TraceParent, IReadOnlyList<Guid> EventIdsSeen);
}
