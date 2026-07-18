using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using UserManager.Application.Events.Interfaces;

namespace UserManager.Infrastructure.Messaging;

public class RabbitMqAuditEventPublisher : IAuditEventPublisher
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqAuditEventPublisher> _logger;

    public RabbitMqAuditEventPublisher(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqAuditEventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string eventType,
                                   string aggregateId,
                                   string aggregateType,
                                   string performedBy,
                                   DateTime occurredAt,
                                   string payloadJson,
                                   CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        using var connection = await factory.CreateConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(exchange: _settings.ExchangeName,
                                           type: ExchangeType.Topic,
                                           durable: true,
                                           cancellationToken: cancellationToken);

        var envelope = new
        {
            eventId = Guid.NewGuid(),
            eventType,
            aggregateId,
            aggregateType,
            performedBy,
            performedAt = occurredAt,
            payload = JsonSerializer.Deserialize<object>(payloadJson)
        };

        var json = JsonSerializer.Serialize(envelope);
        var body = Encoding.UTF8.GetBytes(json);
        var routingKey = $"audit.{eventType}";

        await channel.BasicPublishAsync(exchange: _settings.ExchangeName,
                                        routingKey: routingKey,
                                        body: body,
                                        cancellationToken: cancellationToken);

        _logger.LogInformation("Evento de auditoria publicado: {EventType} | AggregateId: {AggregateId}", eventType, aggregateId);
    }
}