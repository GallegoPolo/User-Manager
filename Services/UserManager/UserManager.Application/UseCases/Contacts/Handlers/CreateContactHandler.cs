using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UseCases.Contacts.Handlers;

public sealed class CreateContactHandler : IRequestHandler<CreateContactCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateContactHandler(IUserRepository userRepository, IContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateContactCommand command, CancellationToken cancellationToken)
    {
        if (!await _userRepository.ExistsAsync(command.OwnerId, cancellationToken))
            return Result<Guid>.NotFound("UserId", "Usuário não encontrado.");

        var email = string.IsNullOrWhiteSpace(command.Email) ? null : Email.Create(command.Email);
        var phone = string.IsNullOrWhiteSpace(command.Phone) ? null : PhoneNumber.Create(command.Phone);

        if (email is not null && await _contactRepository.EmailExistsAsync(command.OwnerId, email, cancellationToken))
            return Result<Guid>.Conflict("Email", "Já existe um contato com este e-mail na agenda.");

        var contact = Contact.Create(command.OwnerId, command.Name, email, phone);

        await _contactRepository.AddAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(contact.Id);
    }
}
