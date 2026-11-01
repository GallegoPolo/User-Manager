using System.Diagnostics;
using AuditService.Domain.Entities;
using AuditService.Domain.Enums;
using AuditService.Domain.Interfaces.Repositories;

namespace AuditService.Infrastructure.IntegrationTests.TestDoubles;

public sealed class FakeAuditLogRepository : IAuditLogRepository
{
    private readonly int _failuresBeforeSuccess;
    private readonly TaskCompletionSource<AuditLog> _saved = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _addCallCount;

    public FakeAuditLogRepository(int failuresBeforeSuccess)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public int AddCallCount => Volatile.Read(ref _addCallCount);

    public string? LastTraceId { get; private set; }

    public Task<bool> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        LastTraceId = Activity.Current?.TraceId.ToString();
        var callNumber = Interlocked.Increment(ref _addCallCount);

        if (callNumber <= _failuresBeforeSuccess)
            throw new InvalidOperationException("Falha transitória simulada.");

        _saved.TrySetResult(auditLog);
        return Task.FromResult(true);
    }

    public Task<AuditLog> WaitForSavedAsync(TimeSpan timeout) => _saved.Task.WaitAsync(timeout);

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
