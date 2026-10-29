namespace UserManager.Api.Contracts.Requests;

public sealed record CreateContactRequest(string Name, string? Email, string? Phone);
