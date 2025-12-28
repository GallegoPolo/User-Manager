using MediatR;
using UserManager.Application.Comon;

namespace UserManager.Application.UseCases.Users.Commands
{
    public record CreateUserCommand(string Name, string Email) : IRequest<Result<Guid>>;
}
