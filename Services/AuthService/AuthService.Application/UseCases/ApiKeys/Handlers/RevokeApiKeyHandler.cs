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

        public RevokeApiKeyHandler(IApiKeyRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
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

            return Result<bool>.Success(true);
        }
    }
}
