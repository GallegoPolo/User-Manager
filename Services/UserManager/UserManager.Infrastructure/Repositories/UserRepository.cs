using Microsoft.EntityFrameworkCore;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManagerDbContext _context;

        public UserRepository(UserManagerDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            return Task.CompletedTask;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<User?> GetByIdentityProviderIdAsync(string identityProviderId, CancellationToken cancellationToken)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.IdentityProviderId == identityProviderId, cancellationToken);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Users.AnyAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }
    }
}
