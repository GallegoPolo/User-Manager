using UserManager.Domain.Interfaces;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Action? OnSave { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnSave?.Invoke();
        SaveCount++;
        return Task.FromResult(1);
    }
}
