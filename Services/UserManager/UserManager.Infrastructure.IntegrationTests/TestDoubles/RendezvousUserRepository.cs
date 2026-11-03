using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;

namespace UserManager.Infrastructure.IntegrationTests.TestDoubles;

public sealed class RendezvousUserRepository : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly Barrier _barrier;
    private bool _alreadyWaited;

    public RendezvousUserRepository(IUserRepository inner, Barrier barrier)
    {
        _inner = inner;
        _barrier = barrier;
    }

    public async Task<User?> GetByIdentityProviderIdAsync(string identityProviderId, CancellationToken cancellationToken)
    {
        var user = await _inner.GetByIdentityProviderIdAsync(identityProviderId, cancellationToken);

        if (user is null && !_alreadyWaited)
        {
            _alreadyWaited = true;
            if (!_barrier.SignalAndWait(TimeSpan.FromSeconds(10), cancellationToken))
                throw new TimeoutException("The two provisioning requests never met at the barrier.");
        }

        return user;
    }

    public Task AddAsync(User user) => _inner.AddAsync(user);
    public Task DeleteAsync(User user) => _inner.DeleteAsync(user);
    public Task<User?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => _inner.ExistsAsync(id, cancellationToken);
    public Task<IEnumerable<User>> GetAllAsync() => _inner.GetAllAsync();
}
