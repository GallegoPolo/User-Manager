using System.Text;
using System.Text.Json;
using AuditService.Application.Events;
using AuditService.Domain.Entities;
using AuditService.Domain.Enums;
using AuditService.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuditService.Infrastructure.Messagings
{
    public class AuditEventConsumer : BackgroundService
    {
        private const int InfiniteDelay = -1;

        private readonly RabbitMqSettings _settings;
        private readonly IAuditLogRepository _repository;
        private readonly ILogger<AuditEventConsumer> _logger;

        private IConnection? _rabbitConnection;
        private IChannel? _rabbitChannel;
        private bool _isDisposed;

        public AuditEventConsumer(IOptions<RabbitMqSettings> settings, IAuditLogRepository repository, ILogger<AuditEventConsumer> logger)
        {
            _settings = settings.Value;
            _repository = repository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await EstablishRabbitMqConnectionAsync(stoppingToken);
                await StartConsumingEventsAsync(stoppingToken);
                await KeepServiceAliveAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumidor de eventos cancelado gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Falha crítica no consumidor de eventos");
                throw;
            }
        }

        private async Task EstablishRabbitMqConnectionAsync(CancellationToken ct)
        {
            var connectionFactory = CreateConnectionFactory();

            _rabbitConnection = await connectionFactory.CreateConnectionAsync(ct);
            _rabbitChannel = await _rabbitConnection.CreateChannelAsync(cancellationToken: ct);

            await DeclareRabbitMqTopologyAsync(ct);
            await ConfigureQualityOfServiceAsync(ct);

            _logger.LogInformation("Conexão com RabbitMQ estabelecida com sucesso");
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };
        }

        private async Task DeclareRabbitMqTopologyAsync(CancellationToken ct)
        {
            await _rabbitChannel!.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: _settings.Durable,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            await _rabbitChannel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: _settings.Durable,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            await _rabbitChannel.QueueBindAsync(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: _settings.RoutingKey,
                arguments: null,
                cancellationToken: ct);
        }

        private async Task ConfigureQualityOfServiceAsync(CancellationToken ct)
        {
            await _rabbitChannel!.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _settings.PrefetchCount,
                global: false,
                cancellationToken: ct);
        }

        private async Task StartConsumingEventsAsync(CancellationToken stoppingToken)
        {
            if (_rabbitChannel is null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized.");

            var eventConsumer = new AsyncEventingBasicConsumer(_rabbitChannel);
            eventConsumer.ReceivedAsync += (sender, deliveryArgs) =>
                HandleEventReceivedAsync(deliveryArgs, stoppingToken);

            await _rabbitChannel.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: eventConsumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Consumidor iniciado. Aguardando eventos na fila: {QueueName}", _settings.QueueName);
        }

        private async Task HandleEventReceivedAsync(BasicDeliverEventArgs deliveryArgs, CancellationToken ct)
        {
            try
            {
                var auditEvent = DeserializeEvent(deliveryArgs);

                if (auditEvent is null)
                {
                    await RejectInvalidEventAsync(deliveryArgs, ct);
                    return;
                }

                var auditLog = CreateAuditLogFromEvent(auditEvent);
                var wasSaved = await _repository.AddAsync(auditLog, ct);

                await AcknowledgeEventAsync(deliveryArgs, auditEvent, wasSaved, ct);
            }
            catch (Exception ex)
            {
                await RequeueEventForRetryAsync(deliveryArgs, ex, ct);
            }
        }

        private AuditEventBase? DeserializeEvent(BasicDeliverEventArgs deliveryArgs)
        {
            var messageBody = deliveryArgs.Body.ToArray();
            var messageText = Encoding.UTF8.GetString(messageBody);

            _logger.LogDebug("Evento recebido da rota: {RoutingKey}", deliveryArgs.RoutingKey);

            return JsonSerializer.Deserialize<AuditEventBase>(messageText);
        }

        private AuditLog CreateAuditLogFromEvent(AuditEventBase auditEvent)
        {
            var eventType = ConvertToEventTypeEnum(auditEvent.EventType);

            return AuditLog.Create(
                auditEvent.EventId,
                auditEvent.AggregateId,
                auditEvent.AggregateType,
                eventType,
                auditEvent.PerformedBy,
                auditEvent.PerformedAt,
                auditEvent.Payload
            );
        }

        private EEventType ConvertToEventTypeEnum(string eventTypeString)
        {
            return eventTypeString switch
            {
                "user.created" => EEventType.UserCreated,
                "user.updated" => EEventType.UserUpdated,
                "user.deleted" => EEventType.UserDeleted,
                "admin.logged_in" => EEventType.AdminLoggedIn,
                "admin.created" => EEventType.AdminCreated,
                "api_key.created" => EEventType.ApiKeyCreated,
                "api_key.revoked" => EEventType.ApiKeyRevoked,
                _ => throw new ArgumentException($"Tipo de evento desconhecido: {eventTypeString}", nameof(eventTypeString))
            };
        }

        private async Task RejectInvalidEventAsync(BasicDeliverEventArgs deliveryArgs, CancellationToken ct)
        {
            _logger.LogWarning("Evento rejeitado: corpo da mensagem inválido ou nulo");
            await _rabbitChannel!.BasicNackAsync(
                deliveryTag: deliveryArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: ct);
        }

        private async Task AcknowledgeEventAsync(
            BasicDeliverEventArgs deliveryArgs,
            AuditEventBase auditEvent,
            bool wasSaved,
            CancellationToken ct)
        {
            if (wasSaved)
            {
                _logger.LogInformation(
                    "Evento processado com sucesso: {EventId} | Tipo: {EventType}",
                    auditEvent.EventId,
                    auditEvent.EventType);
            }
            else
            {
                _logger.LogInformation(
                    "Evento duplicado ignorado: {EventId}",
                    auditEvent.EventId);
            }

            await _rabbitChannel!.BasicAckAsync(
                deliveryTag: deliveryArgs.DeliveryTag,
                multiple: false,
                cancellationToken: ct);
        }

        private async Task RequeueEventForRetryAsync(BasicDeliverEventArgs deliveryArgs, Exception ex, CancellationToken ct)
        {
            _logger.LogError(
                ex,
                "Erro ao processar evento. DeliveryTag: {DeliveryTag}. Evento será reenfileirado",
                deliveryArgs.DeliveryTag);

            await _rabbitChannel!.BasicNackAsync(
                deliveryTag: deliveryArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: ct);
        }

        private static async Task KeepServiceAliveAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(InfiniteDelay, stoppingToken);
        }

        public override void Dispose()
        {
            if (_isDisposed)
                return;

            CloseRabbitMqResourcesSafely();

            _isDisposed = true;
            base.Dispose();
        }

        private void CloseRabbitMqResourcesSafely()
        {
            try
            {
                _rabbitChannel?.CloseAsync().GetAwaiter().GetResult();
                _rabbitConnection?.CloseAsync().GetAwaiter().GetResult();

                _logger.LogInformation("Recursos RabbitMQ fechados com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar recursos RabbitMQ durante dispose");
            }
        }
    }
}
