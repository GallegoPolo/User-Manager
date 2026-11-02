using AuditService.Application.Commons;
using AuditService.Domain.Entities;
using AuditService.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace AuditService.Infrastructure.Messagings;

public sealed class AuditLogRecorder
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogRecorder> _logger;

    public AuditLogRecorder(IAuditLogRepository repository, ILogger<AuditLogRecorder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RecordAsync(Result<AuditLog> mapped, CancellationToken cancellationToken)
    {
        if (!mapped.IsSuccess)
            throw new InvalidAuditEventException(mapped.Error!);

        var auditLog = mapped.Data!;
        var wasSaved = await _repository.AddAsync(auditLog, cancellationToken);

        if (wasSaved)
            _logger.LogInformation("Evento processado com sucesso: {EventId} | Tipo: {EventType}", auditLog.EventId, auditLog.EventType);
        else
            _logger.LogInformation("Evento duplicado ignorado: {EventId}", auditLog.EventId);
    }
}
