using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers;

public sealed class RenameCurrentUserHandler : IRequestHandler<RenameCurrentUserCommand, Result<Unit>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public RenameCurrentUserHandler(ICurrentUser currentUser, IUserRepository users, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(RenameCurrentUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId);

        if (user is null)
            return Result<Unit>.NotFound("Id", "Usuário não encontrado.");

        user.Rename(command.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
