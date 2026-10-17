using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UserManager.Application.Events.Interfaces;

namespace UserManager.Infrastructure.Outbox;

public class OutboxProcessorBackgroundService : BackgroundService
{
    private const int PollIntervalSeconds = 5;
    private const int BatchSize = 20;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

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
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no ciclo da Outbox. Nova tentativa em {PollIntervalSeconds}s.", PollIntervalSeconds);
            }

            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var outboxRepository = scope.ServiceProvider.GetRequiredService<OutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IAuditEventPublisher>();

        var claimedMessages = await outboxRepository.ClaimPendingAsync(BatchSize, LeaseDuration, cancellationToken);

        foreach (var message in claimedMessages)
        {
            try
            {
                await publisher.PublishAsync(eventId: message.Id,
                                             eventType: message.EventType,
                                             aggregateId: message.AggregateId,
                                             aggregateType: message.AggregateType,
                                             performedBy: "system", // TODO: JWT no UserManager ainda não é obrigatório
                                             occurredAt: message.OccurredAt,
                                             payloadJson: message.PayloadJson,
                                             cancellationToken: cancellationToken);

                await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Não marca como processada, tenta de novo no próximo ciclo.
                // TODO: (poison message), não implementado agora.
                _logger.LogWarning(ex, "Falha ao publicar mensagem da Outbox {MessageId}. Será retentada.", message.Id);
            }
        }
    }
}