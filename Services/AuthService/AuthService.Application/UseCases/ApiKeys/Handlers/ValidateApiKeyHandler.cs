using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Application.UseCases.ApiKeys.Queries;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class ValidateApiKeyHandler : IRequestHandler<ValidateApiKeyQuery, Result<ValidateApiKeyDTO>>
    {
        private readonly IApiKeyRepository _repository;
        private readonly IApiKeyDomainService _domainService;
        private readonly IApiKeyHasher _hasher;
        private readonly ITokenGenerator _tokenGenerator;

        public ValidateApiKeyHandler(
         IApiKeyRepository repository,
         IApiKeyDomainService domainService,
         IApiKeyHasher hasher,
         ITokenGenerator tokenGenerator)
        {
            _repository = repository;
            _domainService = domainService;
            _hasher = hasher;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<Result<ValidateApiKeyDTO>> Handle(ValidateApiKeyQuery query, CancellationToken cancellationToken)
        {
            if (!_domainService.ValidateApiKeyFormat(query.ApiKey))
                return Result<ValidateApiKeyDTO>.Failure("ApiKey", "Invalid API Key format");

            var keyHash = _hasher.Hash(query.ApiKey);

            var apiKey = await _repository.GetByHashAsync(keyHash.Value, cancellationToken);

            if (apiKey == null)
                return Result<ValidateApiKeyDTO>.Failure("ApiKey", "Invalid API Key");

            if (!apiKey.IsActive())
                return Result<ValidateApiKeyDTO>.Failure("ApiKey", "API Key is not active or has expired");

            var token = await _tokenGenerator.GenerateTokenAsync(subject: apiKey.Id.ToString(),
                                                                 roles: new[] { "Service" },
                                                                 scopes: apiKey.Scopes.Select(s => s.Value),
                                                                 cancellationToken: cancellationToken);

            var dto = new ValidateApiKeyDTO{Token = token, ExpiresAt = DateTime.UtcNow.AddHours(1)};

            return Result<ValidateApiKeyDTO>.Success(dto);
        }
    }
}
