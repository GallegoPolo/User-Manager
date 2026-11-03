using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Contacts.Handlers;

public sealed class CreateContactHandler : IRequestHandler<CreateContactCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateContactHandler(ICurrentUser currentUser, IUserRepository userRepository, IContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateContactCommand command, CancellationToken cancellationToken)
    {
        if (!await _userRepository.ExistsAsync(_currentUser.UserId, cancellationToken))
            return Result<Guid>.NotFound("UserId", "Usuário não encontrado.");

        var email = command.ToEmail();
        var phone = command.ToPhoneNumber();

        if (email is not null && await _contactRepository.EmailExistsAsync(_currentUser.UserId, email, exceptContactId: null, cancellationToken))
            return Result<Guid>.Conflict("Email", "Já existe um contato com este e-mail na agenda.");

        var contact = Contact.Create(_currentUser.UserId, command.Name, email, phone);

        await _contactRepository.AddAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(contact.Id);
    }
}
