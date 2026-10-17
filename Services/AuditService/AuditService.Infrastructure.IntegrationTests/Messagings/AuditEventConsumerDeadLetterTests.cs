using System.Text;
using System.Text.Json;
using AuditService.Domain.Interfaces.Repositories;
using AuditService.Infrastructure.IntegrationTests.TestDoubles;
using AuditService.Infrastructure.Messagings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AuditService.Infrastructure.IntegrationTests.Messagings;

public sealed class AuditEventConsumerDeadLetterTests : IClassFixture<RabbitMqFixture>
{
    private const string RoutingKey = "audit.user.created";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly RabbitMqFixture _rabbitMq;
    private readonly RabbitMqSettings _settings;

    public AuditEventConsumerDeadLetterTests(RabbitMqFixture rabbitMq)
    {
        _rabbitMq = rabbitMq;
        _settings = rabbitMq.CreateSettings();
    }

    [Fact]
    public async Task InvalidMessage_IsDeadLetteredAndNotPersisted()
    {
        // Arrange
        var repository = new FakeAuditLogRepository(failuresBeforeSuccess: 0);
        await using var services = BuildServices(repository);
        using var consumer = CreateConsumer(services);

        await consumer.StartAsync(CancellationToken.None);
        await WaitUntilConsumingAsync();

        // Act
        await PublishAsync(Encoding.UTF8.GetBytes("isto não é json"));

        // Assert
        var deadLettered = await WaitForDeadLetteredMessageAsync();
        var death = FirstDeathRecord(deadLettered);

        Assert.Equal("rejected", ReadString(death, "reason"));
        Assert.Equal(_settings.QueueName, ReadString(death, "queue"));
        Assert.Equal(0u, await CountMessagesAsync(_settings.QueueName));
        Assert.Equal(0, repository.AddCallCount);

        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TransientFailure_IsRequeuedAndNotDeadLettered()
    {
        // Arrange
        var repository = new FakeAuditLogRepository(failuresBeforeSuccess: 1);
        await using var services = BuildServices(repository);
        using var consumer = CreateConsumer(services);

        await consumer.StartAsync(CancellationToken.None);
        await WaitUntilConsumingAsync();

        // Act
        await PublishAsync(ValidMessageBody());

        // Assert
        await repository.WaitForAddCallsAsync(count: 2, WaitTimeout);

        Assert.Equal(2, repository.AddCallCount);
        Assert.Equal(0u, await CountMessagesAsync(_settings.DeadLetterQueueName));

        await consumer.StopAsync(CancellationToken.None);
    }

    private static byte[] ValidMessageBody()
    {
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventId = Guid.NewGuid(),
            eventType = "user.created",
            aggregateId = Guid.NewGuid().ToString(),
            aggregateType = "User",
            performedBy = "system",
            performedAt = DateTime.UtcNow,
            payload = new { UserName = "Ana", UserEmail = "ana@example.com" }
        });
    }

    private static ServiceProvider BuildServices(FakeAuditLogRepository repository)
    {
        return new ServiceCollection()
            .AddSingleton<IAuditLogRepository>(repository)
            .BuildServiceProvider();
    }

    private AuditEventConsumer CreateConsumer(ServiceProvider services)
    {
        return new AuditEventConsumer(Options.Create(_settings),
                                      services.GetRequiredService<IServiceScopeFactory>(),
                                      NullLogger<AuditEventConsumer>.Instance);
    }

    private async Task PublishAsync(byte[] body)
    {
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.BasicPublishAsync(_settings.ExchangeName, RoutingKey, body);
    }

    private async Task WaitUntilConsumingAsync()
    {
        await using var connection = await _rabbitMq.CreateConnectionAsync();

        await WaitUntilAsync(() => HasActiveConsumerAsync(connection),
                             $"consumidor ativo em {_settings.QueueName}");
    }

    private async Task<bool> HasActiveConsumerAsync(IConnection connection)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync();
            var queue = await channel.QueueDeclarePassiveAsync(_settings.QueueName);
            return queue.ConsumerCount > 0;
        }
        catch (OperationInterruptedException)
        {
            return false;
        }
    }

    private async Task<BasicGetResult> WaitForDeadLetteredMessageAsync()
    {
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        BasicGetResult? message = null;

        await WaitUntilAsync(async () =>
                             {
                                 message = await channel.BasicGetAsync(_settings.DeadLetterQueueName, autoAck: true);
                                 return message is not null;
                             },
                             $"mensagem em {_settings.DeadLetterQueueName}");

        return message!;
    }

    private async Task<uint> CountMessagesAsync(string queueName)
    {
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var queue = await channel.QueueDeclarePassiveAsync(queueName);
        return queue.MessageCount;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, string description)
    {
        var deadline = DateTime.UtcNow + WaitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(PollInterval);
        }

        throw new TimeoutException($"Tempo esgotado aguardando: {description}.");
    }

    private static IDictionary<string, object?> FirstDeathRecord(BasicGetResult message)
    {
        var headers = Assert.IsAssignableFrom<IDictionary<string, object?>>(message.BasicProperties.Headers);
        var deaths = Assert.IsAssignableFrom<IList<object?>>(headers["x-death"]);

        return Assert.IsAssignableFrom<IDictionary<string, object?>>(deaths[0]);
    }

    private static string ReadString(IDictionary<string, object?> record, string key)
    {
        return Encoding.UTF8.GetString(Assert.IsType<byte[]>(record[key]));
    }
}
