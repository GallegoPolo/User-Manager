using AuditService.Domain.Entities;
using AuditService.Domain.Enums;

namespace AuditService.Domain.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task<bool> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
        Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<AuditLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<(List<AuditLog> Logs, long TotalCount)> GetAllAsync(string? aggregateId = null,
                                                                 EEventType? eventType = null,
                                                                 DateTime? fromDate = null,
                                                                 DateTime? toDate = null,
                                                                 int pageNumber = 1,
                                                                 int pageSize = 20,
                                                                 CancellationToken cancellationToken = default);
    }
}
