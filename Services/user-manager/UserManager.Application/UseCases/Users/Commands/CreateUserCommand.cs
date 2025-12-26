using MediatR;
using UserManager.Application.UseCases.Users.Results;

namespace UserManager.Application.UseCases.Users.Commands
{
    public record CreateUserCommand(string Name, string Email)  : IRequest<CreateUserResult>; 
}
