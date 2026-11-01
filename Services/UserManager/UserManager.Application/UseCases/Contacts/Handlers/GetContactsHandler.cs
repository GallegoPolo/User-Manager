using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.DTOs;
using UserManager.Application.UseCases.Contacts.Queries;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Contacts.Handlers;

public sealed class GetContactsHandler : IRequestHandler<GetContactsQuery, Result<IReadOnlyList<ContactDTO>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IContactRepository _contactRepository;

    public GetContactsHandler(IUserRepository userRepository, IContactRepository contactRepository)
    {
        _userRepository = userRepository;
        _contactRepository = contactRepository;
    }

    public async Task<Result<IReadOnlyList<ContactDTO>>> Handle(GetContactsQuery query, CancellationToken cancellationToken)
    {
        if (!await _userRepository.ExistsAsync(query.OwnerId, cancellationToken))
            return Result<IReadOnlyList<ContactDTO>>.NotFound("UserId", "Usuário não encontrado.");

        var contacts = await _contactRepository.GetByOwnerAsync(query.OwnerId, cancellationToken);

        IReadOnlyList<ContactDTO> dtos = contacts.Select(ContactDTO.From).ToList();

        return Result<IReadOnlyList<ContactDTO>>.Success(dtos);
    }
}
