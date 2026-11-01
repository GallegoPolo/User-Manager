using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Contacts.Commands;

public sealed record UpdateContactCommand(Guid OwnerId, Guid ContactId, string Name, string? Email, string? Phone) : IRequest<Result<Unit>>, IContactDetails;
