using System.Security.Claims;
using UserManager.Application.Common;

namespace UserManager.Api.Identity;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _userId;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string IdentityProviderId => Principal.FindFirstValue("sub") ?? string.Empty;

    public string? Email => Principal.FindFirstValue("email");

    public string? Name => Principal.FindFirstValue("name") ?? Principal.FindFirstValue("preferred_username");

    public bool IsProvisioned => _userId.HasValue;

    public Guid UserId => _userId ?? throw new InvalidOperationException("Current user not provisioned.");

    public void MarkProvisioned(Guid userId) => _userId = userId;

    private ClaimsPrincipal Principal =>
        _httpContextAccessor.HttpContext?.User ?? throw new InvalidOperationException("No HTTP request in progress.");
}
