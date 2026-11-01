using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;

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

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_existingIds.Contains(id));

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Any(u => u.Email == email));

    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user) => throw new NotSupportedException();
    public Task DeleteAsync(User user) => throw new NotSupportedException();
    public Task<User?> GetByIdAsync(Guid id) => throw new NotSupportedException();
    public Task<IEnumerable<User>> GetAllAsync() => throw new NotSupportedException();
}
