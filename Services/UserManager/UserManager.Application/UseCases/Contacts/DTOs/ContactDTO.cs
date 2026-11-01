using UserManager.Domain.Entities;

namespace UserManager.Application.UseCases.Contacts.DTOs;

public sealed record ContactDTO(Guid Id, string Name, string? Email, string? Phone, DateTime CreatedAt)
{
    public static ContactDTO From(Contact contact) =>
        new(contact.Id, contact.Name, contact.Email?.Value, contact.Phone?.Value, contact.CreatedAt);
}
