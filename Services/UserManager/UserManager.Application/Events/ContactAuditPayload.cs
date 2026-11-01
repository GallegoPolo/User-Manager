namespace UserManager.Application.Events;

public sealed record ContactAuditPayload(Guid OwnerId, string Name, string? Email, string? Phone);
