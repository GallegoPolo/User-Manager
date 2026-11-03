using UserManager.Application.Common;

namespace UserManager.Infrastructure.IntegrationTests.TestDoubles;

public sealed class FixedCurrentUser : ICurrentUser
{
    public FixedCurrentUser(string identityProviderId, string email, string name)
    {
        IdentityProviderId = identityProviderId;
        Email = email;
        Name = name;
    }

    public string IdentityProviderId { get; }
    public string? Email { get; }
    public string? Name { get; }
    public bool IsProvisioned => false;
    public Guid UserId => throw new InvalidOperationException("Current user not provisioned.");
}
