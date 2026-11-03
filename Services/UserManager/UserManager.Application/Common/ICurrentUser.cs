namespace UserManager.Application.Common;

public interface ICurrentUser
{
    string IdentityProviderId { get; }
    string? Email { get; }
    string? Name { get; }
    bool IsProvisioned { get; }
    Guid UserId { get; }
}
