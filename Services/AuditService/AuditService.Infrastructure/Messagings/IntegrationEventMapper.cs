using AuditService.Application.Commons;
using AuditService.Domain.Entities;
using AuditService.Domain.Enums;
using UserManager.Contracts;

namespace AuditService.Infrastructure.Messagings;

public static class IntegrationEventMapper
{
    private const string SystemActor = "system";
    private const string UserAggregate = "User";
    private const string ContactAggregate = "Contact";

    public static Result<AuditLog> ToAuditLog(UserCreated message) =>
        ToUserAuditLog(message.EventId, message.OccurredAt, message.UserId, EEventType.UserCreated, message.Name, message.Email);

    public static Result<AuditLog> ToAuditLog(UserUpdated message) =>
        ToUserAuditLog(message.EventId, message.OccurredAt, message.UserId, EEventType.UserUpdated, message.Name, message.Email);

    public static Result<AuditLog> ToAuditLog(UserDeleted message) =>
        ToUserAuditLog(message.EventId, message.OccurredAt, message.UserId, EEventType.UserDeleted, message.Name, message.Email);

    public static Result<AuditLog> ToAuditLog(ContactCreated message) =>
        ToContactAuditLog(message.EventId, message.OccurredAt, message.ContactId, EEventType.ContactCreated, message.OwnerId, message.Name, message.Email, message.Phone);

    public static Result<AuditLog> ToAuditLog(ContactUpdated message) =>
        ToContactAuditLog(message.EventId, message.OccurredAt, message.ContactId, EEventType.ContactUpdated, message.OwnerId, message.Name, message.Email, message.Phone);

    public static Result<AuditLog> ToAuditLog(ContactDeleted message) =>
        ToContactAuditLog(message.EventId, message.OccurredAt, message.ContactId, EEventType.ContactDeleted, message.OwnerId, message.Name, message.Email, message.Phone);

    private static Result<AuditLog> ToUserAuditLog(Guid eventId, DateTimeOffset occurredAt, Guid userId, EEventType eventType, string name, string email)
    {
        if (userId == Guid.Empty)
            return Result<AuditLog>.Failure($"EventId {eventId}: UserId não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(name))
            return Result<AuditLog>.Failure($"EventId {eventId}: Name é obrigatório.");

        var payload = new Dictionary<string, object> { ["UserName"] = name, ["UserEmail"] = email };

        return Create(eventId, occurredAt, userId, UserAggregate, eventType, payload);
    }

    private static Result<AuditLog> ToContactAuditLog(Guid eventId,
                                                      DateTimeOffset occurredAt,
                                                      Guid contactId,
                                                      EEventType eventType,
                                                      Guid ownerId,
                                                      string name,
                                                      string? email,
                                                      string? phone)
    {
        if (contactId == Guid.Empty)
            return Result<AuditLog>.Failure($"EventId {eventId}: ContactId não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(name))
            return Result<AuditLog>.Failure($"EventId {eventId}: Name é obrigatório.");

        var payload = new Dictionary<string, object> { ["OwnerId"] = ownerId.ToString(), ["Name"] = name };

        if (email is not null)
            payload["Email"] = email;

        if (phone is not null)
            payload["Phone"] = phone;

        return Create(eventId, occurredAt, contactId, ContactAggregate, eventType, payload);
    }

    private static Result<AuditLog> Create(Guid eventId,
                                           DateTimeOffset occurredAt,
                                           Guid aggregateId,
                                           string aggregateType,
                                           EEventType eventType,
                                           Dictionary<string, object> payload)
    {
        if (eventId == Guid.Empty)
            return Result<AuditLog>.Failure("EventId não pode ser vazio.");

        return Result<AuditLog>.Success(AuditLog.Create(eventId,
                                                        aggregateId.ToString(),
                                                        aggregateType,
                                                        eventType,
                                                        SystemActor,
                                                        occurredAt.UtcDateTime,
                                                        payload));
    }
}
