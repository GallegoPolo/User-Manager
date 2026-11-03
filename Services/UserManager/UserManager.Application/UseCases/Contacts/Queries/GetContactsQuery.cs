using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.DTOs;

namespace UserManager.Application.UseCases.Contacts.Queries;

public sealed record GetContactsQuery() : IRequest<Result<IReadOnlyList<ContactDTO>>>;
