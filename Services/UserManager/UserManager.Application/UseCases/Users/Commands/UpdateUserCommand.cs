using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Users.Commands
{
    public record UpdateUserCommand(Guid Id, string Name, string Email) : IRequest<Result<Guid>>;
}
