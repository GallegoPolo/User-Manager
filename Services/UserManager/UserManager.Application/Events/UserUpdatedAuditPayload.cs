namespace UserManager.Application.Events;

public sealed record UserUpdatedAuditPayload(string UserName, string UserEmail);