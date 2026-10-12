using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using UserManager.Infrastructure.Messaging;

namespace UserManager.Infrastructure.IntegrationTests.Messaging;

public sealed class RabbitMqAuditEventPublisherTests : IClassFixture<RabbitMqFixture>
{
    private const string AuditRoutingPattern = "audit.#";
    private const ushort NoRouteReplyCode = 312;

    private readonly RabbitMqFixture _rabbitMq;

    public RabbitMqAuditEventPublisherTests(RabbitMqFixture rabbitMq)
    {
        _rabbitMq = rabbitMq;
    }

    [Fact]
    public async Task PublishAsync_WhenNoQueueIsBound_ThrowsNoRouteReturn()
    {
        // Arrange
        var publisher = CreatePublisher(exchangeName: UniqueName("exchange"));

        // Act
        var exception = await Assert.ThrowsAsync<PublishReturnException>(() => PublishSampleEventAsync(publisher, Guid.NewGuid()));

        // Assert
        Assert.Equal(NoRouteReplyCode, exception.ReplyCode);
    }

    [Fact]
    public async Task PublishAsync_WhenQueueIsBound_DeliversPersistentMessage()
    {
        // Arrange
        var exchangeName = UniqueName("exchange");
        var queueName = UniqueName("queue");
        await DeclareBoundQueueAsync(exchangeName, queueName);

        var publisher = CreatePublisher(exchangeName);
        var eventId = Guid.NewGuid();

        // Act
        await PublishSampleEventAsync(publisher, eventId);

        // Assert
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var delivered = await channel.BasicGetAsync(queueName, autoAck: true);

        Assert.NotNull(delivered);
        Assert.True(delivered.BasicProperties.Persistent);
        Assert.Contains(eventId.ToString(), Encoding.UTF8.GetString(delivered.Body.Span));
    }

    private RabbitMqAuditEventPublisher CreatePublisher(string exchangeName)
    {
        var settings = Options.Create(_rabbitMq.CreateSettings(exchangeName));
        return new RabbitMqAuditEventPublisher(settings, NullLogger<RabbitMqAuditEventPublisher>.Instance);
    }

    private static Task PublishSampleEventAsync(RabbitMqAuditEventPublisher publisher, Guid eventId)
    {
        return publisher.PublishAsync(eventId: eventId,
                                      eventType: "user.created",
                                      aggregateId: Guid.NewGuid().ToString(),
                                      aggregateType: "User",
                                      performedBy: "system",
                                      occurredAt: DateTime.UtcNow,
                                      payloadJson: """{"UserName":"Ana","UserEmail":"ana@example.com"}""");
    }

    private async Task DeclareBoundQueueAsync(string exchangeName, string queueName)
    {
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queueName, exchangeName, AuditRoutingPattern);
    }

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
