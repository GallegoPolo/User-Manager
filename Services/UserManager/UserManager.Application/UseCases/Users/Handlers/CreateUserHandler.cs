using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UseCases.Users.Handlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
    {
        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserDomainService _userDomainService;

        public CreateUserHandler(IUserRepository repository, IUnitOfWork unitOfWork, IUserDomainService userDomainService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _userDomainService = userDomainService;
        }

        public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var email = Email.Create(command.Email);

            if (!await _userDomainService.IsEmailUniqueAsync(email, cancellationToken))
                return Result<Guid>.Failure("Email", "Este e-mail já está cadastrado no sistema.");

            var user = User.Create(command.Name, email);

            await _repository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(user.Id);
        }
    }
}
