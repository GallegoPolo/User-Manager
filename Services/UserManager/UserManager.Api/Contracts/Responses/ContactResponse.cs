namespace UserManager.Api.Contracts.Responses;

public sealed record GetContactsResponse(Guid Id, string Name, string? Email, string? Phone, DateTime CreatedAt);
