using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Contacts.Commands;

public sealed record CreateContactCommand(Guid OwnerId, string Name, string? Email, string? Phone) : IRequest<Result<Guid>>, IContactDetails;
