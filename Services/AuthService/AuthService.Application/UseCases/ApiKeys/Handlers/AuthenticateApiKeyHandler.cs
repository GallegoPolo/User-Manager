using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class AuthenticateApiKeyHandler : IRequestHandler<AuthenticateApiKeyCommand, Result<TokenDTO>>
    {
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IApiKeyHasher _apiKeyHasher;
        private readonly ITokenGenerator _tokenGenerator;

        public AuthenticateApiKeyHandler(IApiKeyRepository apiKeyRepository, IApiKeyHasher apiKeyHasher, ITokenGenerator tokenGenerator)
        {
            _apiKeyRepository = apiKeyRepository;
            _apiKeyHasher = apiKeyHasher;
            _tokenGenerator = tokenGenerator;
        }

        //TODO: Vulnerability: Timing attack possible here. Consider using a constant time comparison method.
        //Remove GetAllAsync and implement a method to get ApiKey by hash directly.
        public async Task<Result<TokenDTO>> Handle(AuthenticateApiKeyCommand request, CancellationToken cancellationToken)
        {
            var allApiKeys = await _apiKeyRepository.GetAllAsync(cancellationToken);

            var validApiKey = allApiKeys.FirstOrDefault(ak => _apiKeyHasher.Verify(request.ApiKey, ak.SecretHash) && ak.IsActive());

            if (validApiKey == null)
                return Result<TokenDTO>.Failure("Validate", "Invalid or inactive API Key");

            var scopes = validApiKey.Scopes.Select(s => s.Value).ToList();

            var tokenResult = await _tokenGenerator.GenerateTokenAsync(subject: validApiKey.Id.ToString(),
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
