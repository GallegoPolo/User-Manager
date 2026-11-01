using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.DTOs;

namespace UserManager.Application.UseCases.Contacts.Queries;

public sealed record GetContactByIdQuery(Guid OwnerId, Guid ContactId) : IRequest<Result<ContactDTO>>;
