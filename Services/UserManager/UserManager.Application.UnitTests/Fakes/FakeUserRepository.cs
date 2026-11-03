using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly HashSet<Guid> _existingIds = [];
    private readonly List<User> _users = [];

    public IReadOnlyList<User> Users => _users;

    public FakeUserRepository WithUser(Guid id)
    {
        _existingIds.Add(id);
        return this;
    }

    public FakeUserRepository With(User user)
    {
        _users.Add(user);
        _existingIds.Add(user.Id);
        return this;
    }

    public void ReplaceAllWith(User user)
    {
        _users.Clear();
        _existingIds.Clear();
        With(user);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_existingIds.Contains(id));

    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user)
    {
        _users.Remove(user);
        _existingIds.Remove(user.Id);
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

    public Task<User?> GetByIdentityProviderIdAsync(string identityProviderId, CancellationToken cancellationToken) =>
        Task.FromResult(_users.SingleOrDefault(u => u.IdentityProviderId == identityProviderId));

    public Task<IEnumerable<User>> GetAllAsync() =>
        Task.FromResult<IEnumerable<User>>(_users.ToList());
}
