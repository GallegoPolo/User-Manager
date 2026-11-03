using UserManager.Application.Common;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeCurrentUser : ICurrentUser
{
    private Guid? _userId;

    public string IdentityProviderId { get; init; } = "kc-ana";
    public string? Email { get; init; } = "ana@example.com";
    public string? Name { get; init; } = "Ana";

    public bool IsProvisioned => _userId.HasValue;

    public Guid UserId => _userId ?? throw new InvalidOperationException("Current user not provisioned.");

    public FakeCurrentUser ProvisionedAs(Guid userId)
    {
        _userId = userId;
        return this;
    }
}
