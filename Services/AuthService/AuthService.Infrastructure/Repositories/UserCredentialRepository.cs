using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories
{
    public class UserCredentialRepository : IUserCredentialRepository
    {
        private readonly AuthDbContext _context;

        public UserCredentialRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<UserCredential?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.UserCredentials.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
        }

        public async Task AddAsync(UserCredential credential, CancellationToken cancellationToken = default)
        {
            await _context.UserCredentials.AddAsync(credential, cancellationToken);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.UserCredentials.AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
        }
    }
}
