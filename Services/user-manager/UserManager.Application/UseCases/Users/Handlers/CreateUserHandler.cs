using MediatR;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Results;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
    {
        private readonly IUserRepository _repository;

        public CreateUserHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<CreateUserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var user = User.Create(command.Name, command.Email);

            if (!user.IsValid)
                return new CreateUserResult(null, user.Notifications);

            await _repository.AddAsync(user);

            return new CreateUserResult(user.Id, null);
        }
    }
}
