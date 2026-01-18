using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Queries
{
    public record GetApiKeyByIdQuery(Guid Id) : IRequest<Result<ApiKeyDTO>>;
}
