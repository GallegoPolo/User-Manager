namespace UserManager.Api.Contracts.Requests;

public sealed record UpdateContactRequest(string Name, string? Email, string? Phone);
