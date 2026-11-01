using Audit.Contracts;
using AuditService.Domain.Interfaces.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AuditService.Infrastructure.Messagings;

public sealed class AuditEventConsumer : IConsumer<AuditEventOccurred>
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditEventConsumer> _logger;

    public AuditEventConsumer(IAuditLogRepository repository, ILogger<AuditEventConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AuditEventOccurred> context)
    {
        var mapped = AuditEventMapper.ToAuditLog(context.Message);
        if (!mapped.IsSuccess)
            throw new InvalidAuditEventException(mapped.Error!);

        var auditLog = mapped.Data!;
        var wasSaved = await _repository.AddAsync(auditLog, context.CancellationToken);

        if (wasSaved)
            _logger.LogInformation("Evento processado com sucesso: {EventId} | Tipo: {EventType}", auditLog.EventId, auditLog.EventType);
        else
            _logger.LogInformation("Evento duplicado ignorado: {EventId}", auditLog.EventId);
    }
}
