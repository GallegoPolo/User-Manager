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

            var apiKey = await _cacheService.GetCachedApiKeyAsync(parsedKey.Prefix, cancellationToken);

            if (apiKey == null)
            {
                apiKey = await _apiKeyRepository.GetByPrefixAsync(parsedKey.Prefix, cancellationToken);

                if (apiKey == null)
                    return Result<TokenDTO>.Failure("Validate", "Invalid or inactive API Key");

                await _cacheService.SetCachedApiKeyAsync(parsedKey.Prefix, apiKey, cancellationToken);
            }

            if (!apiKey.IsActive())
            {
                await _cacheService.RemoveCachedApiKeyAsync(parsedKey.Prefix, cancellationToken);
                return Result<TokenDTO>.Failure("Validate", "Invalid or inactive API Key");
            }

            if (!_apiKeyHasher.Verify(parsedKey.Secret, apiKey.SecretHash))
                return Result<TokenDTO>.Failure("Validate", "Invalid or inactive API Key");

            var scopes = apiKey.Scopes.Select(s => s.Value).ToList();

            var tokenResult = await _tokenGenerator.GenerateTokenAsync(subject: apiKey.Id.ToString(),
                                                                       roles: ["SERVICE"],
                                                                       scopes: scopes,
                                                                       additionalClaims: null,
                                                                       cancellationToken: cancellationToken);

            var expiresIn = (int)(tokenResult.ExpiresAt - DateTime.UtcNow).TotalSeconds;

            var tokenDto = new TokenDTO(AccessToken: tokenResult.Token, TokenType: "Bearer", ExpiresIn: expiresIn);

            return Result<TokenDTO>.Success(tokenDto);
        }
    }
}
