using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Users.Commands
{
    public record DeleteUserCommand(Guid Id) : IRequest<Result<bool>>;
}
