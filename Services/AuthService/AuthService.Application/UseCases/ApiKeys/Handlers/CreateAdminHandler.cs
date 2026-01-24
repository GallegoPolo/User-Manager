using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class CreateAdminHandler : IRequestHandler<CreateAdminCommand, Result<Guid>>
    {
        private readonly IUserCredentialRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAdminHandler(
            IUserCredentialRepository userRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
        {
            var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);

            if (emailExists)
                return Result<Guid>.Failure("Email", "Email already exists");

            var passwordHash = _passwordHasher.Hash(request.Password);

            var userCredential = UserCredential.Create(request.Email, passwordHash);

            if (!userCredential.IsValid)
                return Result<Guid>.Failure(userCredential.Notifications.ToValidationErrors());

            await _userRepository.AddAsync(userCredential, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(userCredential.Id);
        }
    }
}
