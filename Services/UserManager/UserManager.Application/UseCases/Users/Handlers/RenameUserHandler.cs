using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers;

public sealed class RenameUserHandler : IRequestHandler<RenameUserCommand, Result<Unit>>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RenameUserHandler(IUserRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(RenameUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(command.Id);

        if (user is null)
            return Result<Unit>.NotFound("Id", "Usuário não encontrado.");

        user.Rename(command.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
