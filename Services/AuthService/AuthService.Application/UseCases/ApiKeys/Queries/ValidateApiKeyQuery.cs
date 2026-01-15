using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Queries
{
    public class ValidateApiKeyQuery : IRequest<Result<ValidateApiKeyDTO>>
    {
        public string ApiKey { get; set; } = string.Empty;

        public ValidateApiKeyQuery(string apiKey)
        {
            ApiKey = apiKey;
        }
    }
}
