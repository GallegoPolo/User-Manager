using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.DTOs;
using UserManager.Application.UseCases.Contacts.Queries;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Contacts.Handlers;

public sealed class GetContactByIdHandler : IRequestHandler<GetContactByIdQuery, Result<ContactDTO>>
{
    private readonly IContactRepository _contactRepository;

    public GetContactByIdHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<Result<ContactDTO>> Handle(GetContactByIdQuery query, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(query.OwnerId, query.ContactId, cancellationToken);

        if (contact is null)
            return Result<ContactDTO>.NotFound("ContactId", "Contato não encontrado.");

        return Result<ContactDTO>.Success(ContactDTO.From(contact));
    }
}
