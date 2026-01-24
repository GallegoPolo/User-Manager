using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Application.UseCases.ApiKeys.Queries;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class ListApiKeysHandler : IRequestHandler<ListApiKeysQuery, Result<IEnumerable<ApiKeyDTO>>>
    {
        private readonly IApiKeyRepository _repository;

        public ListApiKeysHandler(IApiKeyRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IEnumerable<ApiKeyDTO>>> Handle(ListApiKeysQuery query, CancellationToken cancellationToken)
        {
            var apiKeys = await _repository.GetAllAsync(cancellationToken);

            var apiKeysDTO = apiKeys.Select(apiKey => new ApiKeyDTO(apiKey.Id,
                                                                    apiKey.Name,
                                                                    apiKey.Scopes.Select(s => s.Value).ToList(),
                                                                    apiKey.Status,
                                                                    apiKey.ExpiresAt,
                                                                    apiKey.CreatedAt));

            return Result<IEnumerable<ApiKeyDTO>>.Success(apiKeysDTO);
        }
    }
}