namespace UserManager.Application.Events;

public sealed record ContactCreatedAuditPayload(Guid OwnerId, string Name, string? Email, string? Phone);
