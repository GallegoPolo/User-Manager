namespace UserManager.Application.UseCases.Contacts.DTOs;

public sealed record ContactDTO(Guid Id, string Name, string? Email, string? Phone, DateTime CreatedAt);
