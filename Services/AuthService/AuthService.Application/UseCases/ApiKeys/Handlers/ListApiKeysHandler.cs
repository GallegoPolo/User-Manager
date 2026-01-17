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

            var apiKeysDTO = apiKeys.Select(apiKey => new ApiKeyDTO
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Scopes = apiKey.Scopes.Select(s => s.Value).ToList(),
                Status = apiKey.Status,
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt
            });

            return Result<IEnumerable<ApiKeyDTO>>.Success(apiKeysDTO);
        }
    }
}