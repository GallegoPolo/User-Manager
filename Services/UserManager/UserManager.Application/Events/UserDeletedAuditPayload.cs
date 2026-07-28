namespace UserManager.Application.Events;

public sealed record UserDeletedAuditPayload(string UserName, string UserEmail);