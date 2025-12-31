using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Responses;

namespace UserManager.Application.UseCases.Users.Queries
{
    public record GetUserByIdQuery(Guid Id) : IRequest<Result<GetUserByIdResponse>>;
}
