using UserManager.Domain.Interfaces;

namespace UserManager.Domain.Services
{
    public class UserDomainService : IUserDomainService
    {
        private readonly IUserRepository _repository;

        public UserDomainService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken)
        {
            var exists = await _repository.EmailExistsAsync(email, cancellationToken);
            return !exists;
        }
    }
}
