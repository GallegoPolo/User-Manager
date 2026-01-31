using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class RevokeApiKeyHandler : IRequestHandler<RevokeApiKeyCommand, Result<bool>>
    {
        private readonly IApiKeyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApiKeyCacheService _cacheService;

        public RevokeApiKeyHandler(IApiKeyRepository repository, IUnitOfWork unitOfWork, IApiKeyCacheService cacheService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<Result<bool>> Handle(RevokeApiKeyCommand command, CancellationToken cancellationToken)
        {
            var apiKey = await _repository.GetByIdAsync(command.Id, cancellationToken);

            if (apiKey == null)
                return Result<bool>.Failure("Id", "ApiKey not found");

            apiKey.Revoke();

            if (!apiKey.IsValid)
                return Result<bool>.Failure(apiKey.Notifications.ToValidationErrors());

            await _repository.UpdateAsync(apiKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveCachedApiKeyAsync(apiKey.Prefix, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
