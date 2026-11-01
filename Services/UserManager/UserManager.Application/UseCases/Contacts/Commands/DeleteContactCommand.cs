using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Contacts.Commands;

public sealed record DeleteContactCommand(Guid OwnerId, Guid ContactId) : IRequest<Result<Unit>>;
