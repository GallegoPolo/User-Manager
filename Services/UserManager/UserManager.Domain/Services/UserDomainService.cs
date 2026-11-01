using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Services
{
    public class UserDomainService : IUserDomainService
    {
        private readonly IUserRepository _repository;

        public UserDomainService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken)
        {
            var exists = await _repository.EmailExistsAsync(email, cancellationToken);
            return !exists;
        }
    }
}
