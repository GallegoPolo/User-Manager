using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Commands
{
    public record LoginAdminCommand(string Email, string Password) : IRequest<Result<TokenDTO>>;
}
