using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Contacts.Handlers;

public sealed class DeleteContactHandler : IRequestHandler<DeleteContactCommand, Result<Unit>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteContactHandler(ICurrentUser currentUser, IContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(DeleteContactCommand command, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(_currentUser.UserId, command.ContactId, cancellationToken);

        if (contact is null)
            return Result<Unit>.NotFound("ContactId", "Contato não encontrado.");

        contact.Delete();
        _contactRepository.Remove(contact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
