using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.DTOs;

namespace UserManager.Application.UseCases.Users.Queries
{
    public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDTO>>;
}
