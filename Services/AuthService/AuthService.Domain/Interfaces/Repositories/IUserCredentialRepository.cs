using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces.Repositories
{
    public interface IUserCredentialRepository
    {
        Task<UserCredential?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task AddAsync(UserCredential credential, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    }
}
