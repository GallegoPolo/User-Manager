using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Domain.DTOs;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class AuthenticateApiKeyHandler : IRequestHandler<AuthenticateApiKeyCommand, Result<TokenDTO>>
    {
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IApiKeyHasher _apiKeyHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IApiKeyDomainService _domainService;
        private readonly IApiKeyCacheService _cacheService;

        public AuthenticateApiKeyHandler(IApiKeyRepository apiKeyRepository, IApiKeyHasher apiKeyHasher, ITokenGenerator tokenGenerator, IApiKeyDomainService domainService, IApiKeyCacheService cacheService)
        {
            _apiKeyRepository = apiKeyRepository;
            _apiKeyHasher = apiKeyHasher;
            _tokenGenerator = tokenGenerator;
            _domainService = domainService;
            _cacheService = cacheService;
        }

        public async Task<Result<TokenDTO>> Handle(AuthenticateApiKeyCommand request, CancellationToken cancellationToken)
        {
            if (!_domainService.ValidateApiKeyFormat(request.ApiKey))
                return Result<TokenDTO>.Failure("Validate", "Invalid API Key format");

            ParsedApiKeyDTO parsedKey;
            try
            {
                parsedKey = _domainService.ParseApiKey(request.ApiKey);
            }
            catch (ArgumentException)
            {
                return Result<TokenDTO>.Failure("Validate", "Invalid API Key format");
            }

            var cachedApiKey = await _cacheService.GetCachedApiKeyAsync(parsedKey.Prefix, cancellationToken);
            CachedApiKeyDTO apiKeyData;

            if (cachedApiKey == null)
            {
                var apiKeyFromDB = await _apiKeyRepository.GetByPrefixAsync(parsedKey.Prefix, cancellationToken);

                if (apiKeyFromDB == null)
                    return Result<TokenDTO>.Failure("Validate", "Invalid or inactive API Key");

                apiKeyData = new CachedApiKeyDTO(apiKeyFromDB.Id,
                                                 apiKeyFromDB.Name,
                                                 apiKeyFromDB.Prefix,
                                                 apiKeyFromDB.SecretHash.Value,
                                                 apiKeyFromDB.Scopes.Select(s => s.Value).ToList(),
                                                 apiKeyFromDB.Status,
                                                 apiKeyFromDB.ExpiresAt,
                                                 apiKeyFromDB.CreatedAt);

                await _cacheService.SetCachedApiKeyAsync(parsedKey.Prefix, apiKeyData, cancellationToken);
            }
            else
            {
                apiKeyData = cachedApiKey;
            }

            if (!apiKeyData.IsActive)
            {
                await _cacheService.RemoveCachedApiKeyAsync(parsedKey.Prefix, cancellationToken);
                return Result<TokenDTO>.Failure("Validate", "Invalid or inactive API Key");
            }

            var secretHash = new Domain.ValueObjects.ApiKeyHash(apiKeyData.SecretHash);

            if (!_apiKeyHasher.Verify(parsedKey.Secret, secretHash))
                return Result<TokenDTO>.Failure("Validate", "Invalid or inactive API Key");

            var tokenResult = await _tokenGenerator.GenerateTokenAsync(subject: apiKeyData.Id.ToString(),
                                                                       roles: ["SERVICE"],
                                                                       scopes: apiKeyData.Scopes, 
                                                                       additionalClaims: null,
                                                                       cancellationToken: cancellationToken
            );

            var expiresIn = (int)(tokenResult.ExpiresAt - DateTime.UtcNow).TotalSeconds;

            var tokenDto = new TokenDTO(AccessToken: tokenResult.Token,
                                        TokenType: "Bearer",
                                        ExpiresIn: expiresIn
            );

            return Result<TokenDTO>.Success(tokenDto);
        }
    }
}
