using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
    {
        private readonly IUserRepository _repository;

        public CreateUserHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var user = User.Create(command.Name, command.Email);

            if (!user.IsValid)
                return Result<Guid>.Failure(user.Notifications.ToValidationErrors());

            await _repository.AddAsync(user);
            return Result<Guid>.Success(user.Id);
        }
    }
}
