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
            var plainApiKey = _domainService.GenerateApiKey();

            var keyHash = _hasher.Hash(plainApiKey);

            var scopes = request.Scopes.Select(s => new Scope(s)).ToList();

            var apiKey = ApiKey.Create(request.Name, keyHash, scopes, request.ExpiresAt);

            if (!apiKey.IsValid)
                return Result<ApiKeyDTO>.Failure(apiKey.Notifications.ToValidationErrors());

            await _repository.AddAsync(apiKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = new ApiKeyDTO
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Scopes = apiKey.Scopes.Select(s => s.Value).ToList(),
                Status = apiKey.Status.ToString(),
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt
            };

            return Result<ApiKeyDTO>.Success(dto);
        }
    }
}
