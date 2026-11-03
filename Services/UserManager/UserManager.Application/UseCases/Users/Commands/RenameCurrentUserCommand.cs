using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Users.Commands;

public sealed record RenameCurrentUserCommand(string Name) : IRequest<Result<Unit>>;
