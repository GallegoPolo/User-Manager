using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Interfaces;

public interface IContactRepository
{
    Task AddAsync(Contact contact, CancellationToken cancellationToken);
    Task<Contact?> GetByIdAsync(Guid ownerId, Guid contactId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Contact>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Guid ownerId, Email email, Guid? exceptContactId, CancellationToken cancellationToken);
    void Remove(Contact contact);
}
