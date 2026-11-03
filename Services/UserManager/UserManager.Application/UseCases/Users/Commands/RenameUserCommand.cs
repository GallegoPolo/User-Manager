using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Users.Commands;

public sealed record RenameUserCommand(Guid Id, string Name) : IRequest<Result<Unit>>;
