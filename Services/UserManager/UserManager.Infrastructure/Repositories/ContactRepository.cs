using Microsoft.EntityFrameworkCore;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.Repositories;

public sealed class ContactRepository : IContactRepository
{
    private readonly UserManagerDbContext _context;

    public ContactRepository(UserManagerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        await _context.Contacts.AddAsync(contact, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _context.Contacts
            .AsNoTracking()
            .Where(c => c.OwnerId == ownerId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(Guid ownerId, Email email, CancellationToken cancellationToken)
    {
        return _context.Contacts.AnyAsync(c => c.OwnerId == ownerId && c.Email == email, cancellationToken);
    }
}
