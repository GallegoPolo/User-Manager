using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Interfaces
{
    public interface IUserDomainService
    {
        Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken);
    }
}
