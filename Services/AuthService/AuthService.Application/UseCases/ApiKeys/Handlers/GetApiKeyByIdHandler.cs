using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Application.UseCases.ApiKeys.Queries;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class GetApiKeyByIdHandler : IRequestHandler<GetApiKeyByIdQuery, Result<ApiKeyDTO>>
    {
        private readonly IApiKeyRepository _repository;

        public GetApiKeyByIdHandler(IApiKeyRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<ApiKeyDTO>> Handle(GetApiKeyByIdQuery query, CancellationToken cancellationToken)
        {
            var apiKey = await _repository.GetByIdAsync(query.Id, cancellationToken);

            if (apiKey == null)
                return Result<ApiKeyDTO>.Failure("Id", "ApiKey not found");

            var dto = new ApiKeyDTO(id: apiKey.Id,
                                    name: apiKey.Name,
                                    scopes: apiKey.Scopes.Select(s => s.Value).ToList(),
                                    status: apiKey.Status,
                                    expiresAt: apiKey.ExpiresAt,
                                    createdAt: apiKey.CreatedAt,
                                    apiKey: null);

            return Result<ApiKeyDTO>.Success(dto);
        }
    }
}
