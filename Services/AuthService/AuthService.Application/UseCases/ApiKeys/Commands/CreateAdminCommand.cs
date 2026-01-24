using AuthService.Application.Common;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Commands
{
    public record CreateAdminCommand(string Email, string Password) : IRequest<Result<Guid>>;
}
