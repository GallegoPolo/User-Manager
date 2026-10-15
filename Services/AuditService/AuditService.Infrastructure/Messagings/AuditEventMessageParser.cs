using System.Text.Json;
using AuditService.Application.Commons;
using AuditService.Application.Events;
using AuditService.Domain.Entities;
using AuditService.Domain.Enums;

namespace AuditService.Infrastructure.Messagings;

public static class AuditEventMessageParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true
    };

    private static readonly IReadOnlyDictionary<string, EEventType> KnownEventTypes = new Dictionary<string, EEventType>
    {
        ["user.created"] = EEventType.UserCreated,
        ["user.updated"] = EEventType.UserUpdated,
        ["user.deleted"] = EEventType.UserDeleted,
        ["admin.logged_in"] = EEventType.AdminLoggedIn,
        ["admin.created"] = EEventType.AdminCreated,
        ["api_key.created"] = EEventType.ApiKeyCreated,
        ["api_key.revoked"] = EEventType.ApiKeyRevoked
    };

    public static Result<AuditLog> Parse(ReadOnlyMemory<byte> body)
    {
        AuditEventBase? auditEvent;

        try
        {
            auditEvent = JsonSerializer.Deserialize<AuditEventBase>(body.Span, SerializerOptions);
        }
        catch (JsonException ex)
        {
            return Result<AuditLog>.Failure($"JSON inválido: {ex.Message}");
        }

        if (auditEvent is null)
            return Result<AuditLog>.Failure("Corpo da mensagem é nulo.");

        var validationError = Validate(auditEvent);
        if (validationError is not null)
            return Result<AuditLog>.Failure($"EventId {auditEvent.EventId}: {validationError}");

        return Result<AuditLog>.Success(AuditLog.Create(auditEvent.EventId,
                                                        auditEvent.AggregateId,
                                                        auditEvent.AggregateType,
                                                        KnownEventTypes[auditEvent.EventType],
                                                        auditEvent.PerformedBy,
                                                        auditEvent.PerformedAt,
                                                        NormalizePayload(auditEvent.Payload)));
    }

    private static string? Validate(AuditEventBase auditEvent)
    {
        if (auditEvent.EventId == Guid.Empty)
            return "EventId não pode ser vazio.";

        if (!KnownEventTypes.ContainsKey(auditEvent.EventType))
            return $"tipo de evento desconhecido '{auditEvent.EventType}'.";

        if (string.IsNullOrWhiteSpace(auditEvent.AggregateId))
            return "AggregateId é obrigatório.";

        if (string.IsNullOrWhiteSpace(auditEvent.AggregateType))
            return "AggregateType é obrigatório.";

        if (string.IsNullOrWhiteSpace(auditEvent.PerformedBy))
            return "PerformedBy é obrigatório.";

        return null;
    }

    private static Dictionary<string, object>? NormalizePayload(Dictionary<string, object>? payload)
    {
        if (payload is null)
            return null;

        var normalized = new Dictionary<string, object>();
        foreach (var (key, value) in payload)
        {
            var converted = ConvertJsonElementValue(value);
            if (converted is not null)
                normalized[key] = converted;
        }
        return normalized;
    }

    private static object? ConvertJsonElementValue(object? value)
    {
        if (value is not JsonElement element)
            return value;

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(e => ConvertJsonElementValue(e)).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElementValue(p.Value)),
            _ => element.ToString()
        };
    }
}
