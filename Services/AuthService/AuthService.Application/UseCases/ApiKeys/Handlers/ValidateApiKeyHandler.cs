using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Application.UseCases.ApiKeys.Queries;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using MediatR;
using System;
using System.Collections.Generic;

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

            //TODO: Later improvements:
            //Add an index to the owner/user field.
            //Limit the search to active API Keys.
            //Use cache(Redis) for frequently used API Keys.
            var allApiKeys = await _repository.GetAllAsync(cancellationToken);
            ApiKey? validApiKey = null;
            foreach (var storedApiKey in allApiKeys)
            {
                if (_hasher.Verify(query.ApiKey, storedApiKey.KeyHash))
                {
                    validApiKey = storedApiKey;
                    break;
                }
            }

            if (validApiKey == null)
                return Result<ValidateApiKeyDTO>.Failure("ApiKey", "Invalid API Key");

            if (!validApiKey.IsActive())
                return Result<ValidateApiKeyDTO>.Failure("ApiKey", "API Key is not active or has expired");

            var token = await _tokenGenerator.GenerateTokenAsync(
                subject: validApiKey.Id.ToString(),
                roles: new[] { "Service" },
                scopes: validApiKey.Scopes.Select(s => s.Value),
                cancellationToken: cancellationToken);

            var tokenDTO = new ValidateApiKeyDTO
            {
                Token = token.Token,
                ExpiresAt = token.ExpiresAt
            };

            return Result<ValidateApiKeyDTO>.Success(tokenDTO);
        }
    }
}
