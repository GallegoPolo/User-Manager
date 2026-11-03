using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Contacts.Handlers;

public sealed class UpdateContactHandler : IRequestHandler<UpdateContactCommand, Result<Unit>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateContactHandler(ICurrentUser currentUser, IContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(UpdateContactCommand command, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(_currentUser.UserId, command.ContactId, cancellationToken);

        if (contact is null)
            return Result<Unit>.NotFound("ContactId", "Contato não encontrado.");

        var email = command.ToEmail();

        if (email is not null && await _contactRepository.EmailExistsAsync(_currentUser.UserId, email, contact.Id, cancellationToken))
            return Result<Unit>.Conflict("Email", "Já existe um contato com este e-mail na agenda.");

        contact.Update(command.Name, email, command.ToPhoneNumber());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
