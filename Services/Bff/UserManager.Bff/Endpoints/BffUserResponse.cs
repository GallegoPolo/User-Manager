namespace UserManager.Bff.Endpoints;

public sealed record BffUserResponse(string Sub, string? Name, string? Email, string[] Roles, string LogoutUrl);
