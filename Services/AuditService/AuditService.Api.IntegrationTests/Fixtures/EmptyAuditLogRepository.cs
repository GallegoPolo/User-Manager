using AuditService.Domain.Entities;
using AuditService.Domain.Enums;
using AuditService.Domain.Interfaces.Repositories;

namespace AuditService.Api.IntegrationTests.Fixtures;

public sealed class EmptyAuditLogRepository : IAuditLogRepository
{
    public Task<bool> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<AuditLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<AuditLog?>(null);

    public Task<(List<AuditLog> Logs, long TotalCount)> GetAllAsync(string? aggregateId = null,
                                                                    EEventType? eventType = null,
                                                                    DateTime? fromDate = null,
                                                                    DateTime? toDate = null,
                                                                    int pageNumber = 1,
                                                                    int pageSize = 20,
                                                                    CancellationToken cancellationToken = default) =>
        Task.FromResult((new List<AuditLog>(), 0L));
}
