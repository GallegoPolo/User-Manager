using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Queries
{
    public record ValidateApiKeyQuery(string ApiKey) : IRequest<Result<ValidateApiKeyDTO>>;
}
