using AuthService.Application.Common;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Commands
{
    public record RevokeApiKeyCommand(Guid Id) : IRequest<Result<bool>>;
}
