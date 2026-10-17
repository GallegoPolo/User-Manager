using AuditService.Domain.Entities;
using AuditService.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuditService.Infrastructure.Messagings;

public class AuditEventConsumer : BackgroundService
{
    private const int InfiniteDelay = -1;

    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditEventConsumer> _logger;

    private IConnection? _rabbitConnection;
    private IChannel? _rabbitChannel;
    private bool _isDisposed;

    public AuditEventConsumer(IOptions<RabbitMqSettings> settings, IServiceScopeFactory scopeFactory, ILogger<AuditEventConsumer> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var channel = await EstablishRabbitMqConnectionAsync(stoppingToken);
            await StartConsumingEventsAsync(channel, stoppingToken);
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

    private async Task<IChannel> EstablishRabbitMqConnectionAsync(CancellationToken ct)
    {
        var connectionFactory = CreateConnectionFactory();

        var connection = await connectionFactory.CreateConnectionAsync(ct);
        _rabbitConnection = connection;

        var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        _rabbitChannel = channel;

        await DeclareRabbitMqTopologyAsync(channel, ct);
        await ConfigureQualityOfServiceAsync(channel, ct);

        _logger.LogInformation("Conexão com RabbitMQ estabelecida com sucesso");

        return channel;
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

    private async Task DeclareRabbitMqTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await DeclareMainQueueAsync(channel, ct);
        await DeclareDeadLetterQueueAsync(channel, ct);
    }

    private async Task DeclareMainQueueAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: _settings.Durable,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: _settings.QueueName,
            durable: _settings.Durable,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: _settings.QueueName,
            exchange: _settings.ExchangeName,
            routingKey: _settings.RoutingKey,
            arguments: null,
            cancellationToken: ct);
    }

    private async Task DeclareDeadLetterQueueAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: _settings.DeadLetterExchangeName,
            type: ExchangeType.Fanout,
            durable: _settings.Durable,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: _settings.DeadLetterQueueName,
            durable: _settings.Durable,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: _settings.DeadLetterQueueName,
            exchange: _settings.DeadLetterExchangeName,
            routingKey: string.Empty,
            arguments: null,
            cancellationToken: ct);
    }

    private async Task ConfigureQualityOfServiceAsync(IChannel channel, CancellationToken ct)
    {
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _settings.PrefetchCount,
            global: false,
            cancellationToken: ct);
    }

    private async Task StartConsumingEventsAsync(IChannel channel, CancellationToken stoppingToken)
    {
        var eventConsumer = new AsyncEventingBasicConsumer(channel);
        eventConsumer.ReceivedAsync += (sender, deliveryArgs) =>
            HandleEventReceivedAsync(channel, deliveryArgs, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: eventConsumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Consumidor iniciado. Aguardando eventos na fila: {QueueName}", _settings.QueueName);
    }

    private async Task HandleEventReceivedAsync(IChannel channel, BasicDeliverEventArgs deliveryArgs, CancellationToken ct)
    {
        try
        {
            var parseResult = AuditEventMessageParser.Parse(deliveryArgs.Body);

            if (!parseResult.IsSuccess)
            {
                await RejectInvalidEventAsync(channel, deliveryArgs, parseResult.Error!, ct);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            var auditLog = parseResult.Data!;
            var wasSaved = await repository.AddAsync(auditLog, ct);

            await AcknowledgeEventAsync(channel, deliveryArgs, auditLog, wasSaved, ct);
        }
        catch (Exception ex)
        {
            await RequeueEventForRetryAsync(channel, deliveryArgs, ex, ct);
        }
    }

    private async Task RejectInvalidEventAsync(IChannel channel, BasicDeliverEventArgs deliveryArgs, string reason, CancellationToken ct)
    {
        _logger.LogError("Evento rejeitado sem nova tentativa (erro permanente). RoutingKey: {RoutingKey} | Motivo: {Reason}",
                         deliveryArgs.RoutingKey,
                         reason);

        await channel.BasicNackAsync(
            deliveryTag: deliveryArgs.DeliveryTag,
            multiple: false,
            requeue: false,
            cancellationToken: ct);
    }

    private async Task AcknowledgeEventAsync(
        IChannel channel,
        BasicDeliverEventArgs deliveryArgs,
        AuditLog auditLog,
        bool wasSaved,
        CancellationToken ct)
    {
        if (wasSaved)
        {
            _logger.LogInformation(
                "Evento processado com sucesso: {EventId} | Tipo: {EventType}",
                auditLog.EventId,
                auditLog.EventType);
        }
        else
        {
            _logger.LogInformation(
                "Evento duplicado ignorado: {EventId}",
                auditLog.EventId);
        }

        await channel.BasicAckAsync(
            deliveryTag: deliveryArgs.DeliveryTag,
            multiple: false,
            cancellationToken: ct);
    }

    private async Task RequeueEventForRetryAsync(IChannel channel, BasicDeliverEventArgs deliveryArgs, Exception ex, CancellationToken ct)
    {
        _logger.LogError(
            ex,
            "Erro ao processar evento. DeliveryTag: {DeliveryTag}. Evento será reenfileirado",
            deliveryArgs.DeliveryTag);

        await channel.BasicNackAsync(
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
