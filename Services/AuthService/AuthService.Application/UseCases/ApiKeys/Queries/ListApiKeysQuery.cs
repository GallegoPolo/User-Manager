using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Queries
{
    public record ListApiKeysQuery : IRequest<Result<IEnumerable<ApiKeyDTO>>>;
}
