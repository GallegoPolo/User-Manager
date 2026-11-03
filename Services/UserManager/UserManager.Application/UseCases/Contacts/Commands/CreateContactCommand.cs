using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Contacts.Commands;

public sealed record CreateContactCommand(string Name, string? Email, string? Phone) : IRequest<Result<Guid>>, IContactDetails;
