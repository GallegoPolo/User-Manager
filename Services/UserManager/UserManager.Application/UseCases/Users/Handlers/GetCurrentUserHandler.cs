using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.DTOs;
using UserManager.Application.UseCases.Users.Queries;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers;

public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDTO>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;

    public GetCurrentUserHandler(ICurrentUser currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<UserDTO>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId);

        if (user is null)
            return Result<UserDTO>.NotFound("Id", "Usuário não encontrado.");

        return Result<UserDTO>.Success(new UserDTO(user.Id, user.Name, user.Email.Value, user.CreatedAt));
    }
}
