namespace UserManager.Api.Contracts.Responses;

public sealed record ContactResponse(Guid Id, string Name, string? Email, string? Phone, DateTime CreatedAt);
