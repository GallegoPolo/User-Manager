using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly HashSet<Guid> _existingIds = [];

    public FakeUserRepository WithUser(Guid id)
    {
        _existingIds.Add(id);
        return this;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_existingIds.Contains(id));

    public Task AddAsync(User user) => throw new NotSupportedException();
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task UpdateAsync(User user) => throw new NotSupportedException();
    public Task DeleteAsync(User user) => throw new NotSupportedException();
    public Task<User?> GetByIdAsync(Guid id) => throw new NotSupportedException();
    public Task<IEnumerable<User>> GetAllAsync() => throw new NotSupportedException();
}
