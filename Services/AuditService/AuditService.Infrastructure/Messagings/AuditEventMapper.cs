using System.Text.Json;
using Audit.Contracts;
using AuditService.Application.Commons;
using AuditService.Domain.Entities;
using AuditService.Domain.Enums;

namespace AuditService.Infrastructure.Messagings;

public static class AuditEventMapper
{
    private static readonly IReadOnlyDictionary<string, EEventType> KnownEventTypes = new Dictionary<string, EEventType>
    {
        ["user.created"] = EEventType.UserCreated,
        ["user.updated"] = EEventType.UserUpdated,
        ["user.deleted"] = EEventType.UserDeleted,
        ["admin.logged_in"] = EEventType.AdminLoggedIn,
        ["admin.created"] = EEventType.AdminCreated,
        ["api_key.created"] = EEventType.ApiKeyCreated,
        ["api_key.revoked"] = EEventType.ApiKeyRevoked,
        ["contact.created"] = EEventType.ContactCreated,
        ["contact.updated"] = EEventType.ContactUpdated,
        ["contact.deleted"] = EEventType.ContactDeleted
    };

    public static Result<AuditLog> ToAuditLog(AuditEventOccurred message)
    {
        var validationError = Validate(message);
        if (validationError is not null)
            return Result<AuditLog>.Failure($"EventId {message.EventId}: {validationError}");

        return Result<AuditLog>.Success(AuditLog.Create(message.EventId,
                                                        message.AggregateId,
                                                        message.AggregateType,
                                                        KnownEventTypes[message.EventType],
                                                        message.PerformedBy,
                                                        message.PerformedAt,
                                                        ToDictionary(message.Payload)));
    }

    private static string? Validate(AuditEventOccurred message)
    {
        if (message.EventId == Guid.Empty)
            return "EventId não pode ser vazio.";

        if (message.EventType is null || !KnownEventTypes.ContainsKey(message.EventType))
            return $"tipo de evento desconhecido '{message.EventType}'.";

        if (string.IsNullOrWhiteSpace(message.AggregateId))
            return "AggregateId é obrigatório.";

        if (string.IsNullOrWhiteSpace(message.AggregateType))
            return "AggregateType é obrigatório.";

        if (string.IsNullOrWhiteSpace(message.PerformedBy))
            return "PerformedBy é obrigatório.";

        if (message.Payload is { ValueKind: not (JsonValueKind.Object or JsonValueKind.Null) })
            return "Payload precisa ser um objeto JSON.";

        return null;
    }

    private static Dictionary<string, object>? ToDictionary(JsonElement? payload)
    {
        if (payload is not { ValueKind: JsonValueKind.Object } element)
            return null;

        var dictionary = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
        {
            var value = ConvertJsonElementValue(property.Value);
            if (value is not null)
                dictionary[property.Name] = value;
        }
        return dictionary;
    }

    private static object? ConvertJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElementValue(p.Value)),
            _ => element.ToString()
        };
    }
}
