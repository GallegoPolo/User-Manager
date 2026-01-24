using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Commands
{
    public record CreateApiKeyCommand(string Name, IReadOnlyList<string> Scopes, DateTime? ExpiresAt = null) : IRequest<Result<ApiKeyDTO>>;
}
