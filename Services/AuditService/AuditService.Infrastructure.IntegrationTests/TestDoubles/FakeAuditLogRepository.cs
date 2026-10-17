using AuditService.Domain.Entities;
using AuditService.Domain.Enums;
using AuditService.Domain.Interfaces.Repositories;

namespace AuditService.Infrastructure.IntegrationTests.TestDoubles;

public sealed class FakeAuditLogRepository : IAuditLogRepository
{
    private readonly int _failuresBeforeSuccess;
    private int _addCallCount;

    public FakeAuditLogRepository(int failuresBeforeSuccess)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public int AddCallCount => Volatile.Read(ref _addCallCount);

    public Task<bool> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        var callNumber = Interlocked.Increment(ref _addCallCount);

        if (callNumber <= _failuresBeforeSuccess)
            throw new InvalidOperationException("Falha transitória simulada.");

        return Task.FromResult(true);
    }

    public async Task WaitForAddCallsAsync(int count, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (AddCallCount < count)
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException($"Esperava {count} chamadas a AddAsync, houve {AddCallCount}.");

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    public Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<AuditLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<(List<AuditLog> Logs, long TotalCount)> GetAllAsync(string? aggregateId = null,
                                                                    EEventType? eventType = null,
                                                                    DateTime? fromDate = null,
                                                                    DateTime? toDate = null,
                                                                    int pageNumber = 1,
                                                                    int pageSize = 20,
                                                                    CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
