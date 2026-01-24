using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Interfaces.Repositories
{
    public interface IPasswordHasher
    {
        PasswordHash Hash(string plainPassword);
        bool Verify(string plainPassword, PasswordHash passwordHash);
    }
}
