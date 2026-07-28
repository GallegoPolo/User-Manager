using UserManager.Domain.Entities;

namespace UserManager.Domain.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllAsync();
}
