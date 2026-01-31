using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.ValueObjects;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class CreateApiKeyHandler : IRequestHandler<CreateApiKeyCommand, Result<ApiKeyDTO>>
    {
        private readonly IApiKeyRepository _repository;
        private readonly IApiKeyDomainService _domainService;
        private readonly IApiKeyHasher _hasher;
        private readonly IUnitOfWork _unitOfWork;

        public CreateApiKeyHandler(IApiKeyRepository repository, IApiKeyDomainService domainService, IApiKeyHasher hasher, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _domainService = domainService;
            _hasher = hasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ApiKeyDTO>> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
        {
            var generatedKey = _domainService.GenerateApiKey(environment: "live");

            // TODO: Hashing done at the domain service level?
            var secretHash = _hasher.Hash(generatedKey.Secret);

            var scopes = request.Scopes.Select(s => new Scope(s)).ToList();

            var apiKey = ApiKey.Create(name: request.Name,
                                       prefix: generatedKey.Prefix,
                                       secretHash: secretHash,
                                       scopes: scopes,
                                       expiresAt: request.ExpiresAt);

            if (!apiKey.IsValid)
                return Result<ApiKeyDTO>.Failure(apiKey.Notifications.ToValidationErrors());

            await _repository.AddAsync(apiKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = new ApiKeyDTO(id: apiKey.Id,
                                    name: apiKey.Name,
                                    scopes: apiKey.Scopes.Select(s => s.Value).ToList(),
                                    status: apiKey.Status,
                                    expiresAt: apiKey.ExpiresAt,
                                    createdAt: apiKey.CreatedAt,
                                    apiKey: generatedKey.FullKey 
            );

            return Result<ApiKeyDTO>.Success(dto);
        }
    }
}