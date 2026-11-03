namespace UserManager.Api.Contracts.Responses;

public sealed record UserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
