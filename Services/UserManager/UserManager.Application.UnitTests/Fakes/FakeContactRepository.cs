using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeContactRepository : IContactRepository
{
    private readonly List<Contact> _contacts = [];

    public IReadOnlyList<Contact> Contacts => _contacts;

    public FakeContactRepository With(Contact contact)
    {
        _contacts.Add(contact);
        return this;
    }

    public Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        _contacts.Add(contact);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Contact>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Contact> owned = _contacts.Where(c => c.OwnerId == ownerId).ToList();
        return Task.FromResult(owned);
    }

    public Task<bool> EmailExistsAsync(Guid ownerId, Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_contacts.Any(c => c.OwnerId == ownerId && c.Email == email));
}
