using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Audit.Contracts;
using AuditService.Domain.Interfaces.Repositories;
using AuditService.Infrastructure.IntegrationTests.TestDoubles;
using AuditService.Infrastructure.Messagings;
using MassTransit;
using MassTransit.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AuditService.Infrastructure.IntegrationTests.Messagings;

public sealed class AuditEventConsumerTests : IClassFixture<RabbitMqFixture>
{
    private const string FaultMessageHeader = "MT-Fault-Message";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly RabbitMqFixture _rabbitMq;
    private readonly RabbitMqSettings _settings;

    public AuditEventConsumerTests(RabbitMqFixture rabbitMq)
    {
        _rabbitMq = rabbitMq;
        _settings = rabbitMq.CreateSettings($"audit_events_{Guid.NewGuid():N}");
    }

    private string ErrorQueueName => $"{_settings.QueueName}_error";

    [Fact]
    public async Task ValidMessage_IsSaved()
    {
        // Arrange
        var repository = new FakeAuditLogRepository(failuresBeforeSuccess: 0);
        await using var bus = await StartBusAsync(repository);
        var message = ValidMessage();

        // Act
        await bus.Bus.Publish(message);

        // Assert
        var saved = await repository.WaitForSavedAsync(WaitTimeout);
        Assert.Equal(message.EventId, saved.EventId);
    }

    [Fact]
    public async Task InvalidMessage_GoesToErrorQueueWithReasonAndIsNotSaved()
    {
        // Arrange
        var repository = new FakeAuditLogRepository(failuresBeforeSuccess: 0);
        await using var bus = await StartBusAsync(repository);

        // Act
        await bus.Bus.Publish(ValidMessage() with { EventType = "user.archived" });

        // Assert
        var faulted = await WaitForMessageAsync(ErrorQueueName);
        var headers = Assert.IsAssignableFrom<IDictionary<string, object?>>(faulted.BasicProperties.Headers);

        Assert.Contains("user.archived", ReadString(headers, FaultMessageHeader));
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task ValidMessage_IsProcessedInsidePublisherTrace()
    {
        // Arrange
        using var listener = new ActivitySourceListener(DiagnosticHeaders.DefaultListenerName);
        var repository = new FakeAuditLogRepository(failuresBeforeSuccess: 0);
        await using var bus = await StartBusAsync(repository);
        using var publisherActivity = new Activity("publisher").SetIdFormat(ActivityIdFormat.W3C).Start();

        // Act
        await bus.Bus.Publish(ValidMessage());
        publisherActivity.Stop();

        // Assert
        await repository.WaitForSavedAsync(WaitTimeout);
        Assert.Equal(publisherActivity.TraceId.ToString(), repository.LastTraceId);
    }

    private async Task<RunningBus> StartBusAsync(FakeAuditLogRepository repository)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IAuditLogRepository>(repository)
            .AddAuditEventConsumer(_settings)
            .BuildServiceProvider();

        foreach (var hostedService in services.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        await services.GetRequiredService<IBusControl>().WaitForHealthStatus(BusHealthStatus.Healthy, WaitTimeout);

        return new RunningBus(services);
    }

    private static AuditEventOccurred ValidMessage() => new()
    {
        EventId = Guid.NewGuid(),
        EventType = "user.created",
        AggregateId = Guid.NewGuid().ToString(),
        AggregateType = "User",
        PerformedBy = "system",
        PerformedAt = DateTime.UtcNow,
        Payload = JsonSerializer.SerializeToElement(new { UserName = "Ana", UserEmail = "ana@example.com" })
    };

    private async Task<BasicGetResult> WaitForMessageAsync(string queueName)
    {
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        var deadline = DateTime.UtcNow + WaitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var message = await TryGetMessageAsync(connection, queueName);
            if (message is not null)
                return message;

            await Task.Delay(PollInterval);
        }

        throw new TimeoutException($"Tempo esgotado aguardando mensagem em {queueName}.");
    }

    private static async Task<BasicGetResult?> TryGetMessageAsync(IConnection connection, string queueName)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync();
            return await channel.BasicGetAsync(queueName, autoAck: true);
        }
        catch (OperationInterruptedException)
        {
            return null;
        }
    }

    private static string ReadString(IDictionary<string, object?> headers, string key)
    {
        return headers[key] switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            var value => value?.ToString() ?? string.Empty
        };
    }

    private sealed class RunningBus(ServiceProvider services) : IAsyncDisposable
    {
        public IBus Bus => services.GetRequiredService<IBus>();

        public async ValueTask DisposeAsync()
        {
            foreach (var hostedService in services.GetServices<IHostedService>().Reverse())
                await hostedService.StopAsync(CancellationToken.None);

            await services.DisposeAsync();
        }
    }
}
