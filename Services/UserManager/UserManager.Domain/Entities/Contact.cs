using UserManager.Domain.Events.Entities;
using UserManager.Domain.Events.Interfaces;
using UserManager.Domain.Exceptions;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Entities;

public sealed class Contact : IHasDomainEvents
{
    public const int MaxNameLength = 200;

    private readonly List<IDomainEvent> _domainEvents = [];

    private Contact() { }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Email? Email { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Contact Create(Guid ownerId, string name, Email? email, PhoneNumber? phone)
    {
        if (ownerId == Guid.Empty)
            throw new DomainException("O contato precisa pertencer a um usuário.");

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        contact.ApplyDetails(name, email, phone);
        contact._domainEvents.Add(new ContactCreatedDomainEvent(contact.Id, contact.OwnerId, contact.Name, email?.Value, phone?.Value));

        return contact;
    }

    public void Update(string name, Email? email, PhoneNumber? phone)
    {
        ApplyDetails(name, email, phone);
        _domainEvents.Add(new ContactUpdatedDomainEvent(Id, OwnerId, Name, Email?.Value, Phone?.Value));
    }

    public void Delete()
    {
        _domainEvents.Add(new ContactDeletedDomainEvent(Id, OwnerId, Name, Email?.Value, Phone?.Value));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void ApplyDetails(string name, Email? email, PhoneNumber? phone)
    {
        var trimmedName = name?.Trim() ?? string.Empty;

        if (trimmedName.Length == 0)
            throw new DomainException("O nome do contato é obrigatório.");

        if (trimmedName.Length > MaxNameLength)
            throw new DomainException($"O nome do contato deve ter no máximo {MaxNameLength} caracteres.");

        if (email is null && phone is null)
            throw new DomainException("O contato precisa de um e-mail ou de um telefone.");

        Name = trimmedName;
        Email = email;
        Phone = phone;
    }
}
