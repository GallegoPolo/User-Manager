namespace UserManager.Application.Events
{
    public sealed record UserCreatedAuditPayload(string UserName, string UserEmail);
}
