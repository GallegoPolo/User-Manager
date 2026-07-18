using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UserManager.Application.Events.Interfaces;

namespace UserManager.Infrastructure.Outbox;

public class OutboxProcessorBackgroundService : BackgroundService
{
    private const int PollIntervalSeconds = 5;
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingMessagesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var outboxRepository = scope.ServiceProvider.GetRequiredService<OutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IAuditEventPublisher>();

        var pendingMessages = await outboxRepository.GetPendingAsync(BatchSize, cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                await publisher.PublishAsync(eventType: message.EventType,
                                             aggregateId: message.AggregateId,
                                             aggregateType: message.AggregateType,
                                             performedBy: "system", // TODO: JWT no UserManager ainda não é obrigatório
                                             occurredAt: message.OccurredAt,
                                             payloadJson: message.PayloadJson,
                                             cancellationToken: cancellationToken);

                await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                // Não marca como processada, tenta de novo no próximo ciclo.
                // TODO: (poison message), não implementado agora.
                _logger.LogWarning(ex, "Falha ao publicar mensagem da Outbox {MessageId}. Será retentada.", message.Id);
            }
        }
    }
}