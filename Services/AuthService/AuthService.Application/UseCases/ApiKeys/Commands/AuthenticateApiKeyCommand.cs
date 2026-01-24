using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Commands
{
    public record AuthenticateApiKeyCommand(string ApiKey) : IRequest<Result<TokenDTO>>;
}
