using System.Text.Json;
using AuditService.Domain.Enums;
using AuditService.Infrastructure.Messagings;
using UserManager.Contracts;

namespace AuditService.Infrastructure.UnitTests.Messagings;

public sealed class IntegrationEventMapperTests
{
    private static readonly Guid EventId = Guid.Parse("6f1c2a9e-3b4d-4e5f-8a7b-9c0d1e2f3a4b");
    private static readonly Guid UserId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid ContactId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTime OccurredAt = new(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void ToAuditLog_UserCreated_MapsAllFields()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new UserCreated
        {
            EventId = EventId,
            OccurredAt = OccurredAt,
            UserId = UserId,
            Name = "Ana",
            Email = "ana@example.com"
        });

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        var auditLog = result.Data!;
        Assert.Equal(EventId, auditLog.EventId);
        Assert.Equal(UserId.ToString(), auditLog.AggregateId);
        Assert.Equal("User", auditLog.AggregateType);
        Assert.Equal(EEventType.UserCreated, auditLog.EventType);
        Assert.Equal("system", auditLog.PerformedBy);
        Assert.Equal(OccurredAt, auditLog.Timestamp);
        Assert.Equal(new Dictionary<string, object> { ["UserName"] = "Ana", ["UserEmail"] = "ana@example.com" }, auditLog.Payload);
    }

    [Fact]
    public void ToAuditLog_UserUpdated_MapsToUserUpdated()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new UserUpdated { EventId = EventId, OccurredAt = OccurredAt, UserId = UserId, Name = "Ana", Email = "ana@example.com" });

        // Assert
        Assert.Equal(EEventType.UserUpdated, result.Data!.EventType);
    }

    [Fact]
    public void ToAuditLog_UserDeleted_MapsToUserDeleted()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new UserDeleted { EventId = EventId, OccurredAt = OccurredAt, UserId = UserId, Name = "Ana", Email = "ana@example.com" });

        // Assert
        Assert.Equal(EEventType.UserDeleted, result.Data!.EventType);
    }

    [Fact]
    public void ToAuditLog_ContactUpdated_MapsAllFieldsAndOmitsMissingEmail()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new ContactUpdated
        {
            EventId = EventId,
            OccurredAt = OccurredAt,
            ContactId = ContactId,
            OwnerId = UserId,
            Name = "Bia",
            Email = null,
            Phone = "11987654321"
        });

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        var auditLog = result.Data!;
        Assert.Equal(ContactId.ToString(), auditLog.AggregateId);
        Assert.Equal("Contact", auditLog.AggregateType);
        Assert.Equal(EEventType.ContactUpdated, auditLog.EventType);
        Assert.Equal(new Dictionary<string, object> { ["OwnerId"] = UserId.ToString(), ["Name"] = "Bia", ["Phone"] = "11987654321" }, auditLog.Payload);
    }

    [Fact]
    public void ToAuditLog_ContactWithEmailAndPhone_KeepsBothInPayload()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new ContactDeleted
        {
            EventId = EventId,
            OccurredAt = OccurredAt,
            ContactId = ContactId,
            OwnerId = UserId,
            Name = "Bia",
            Email = "bia@example.com",
            Phone = "11987654321"
        });

        // Assert
        Assert.Equal(new Dictionary<string, object>
        {
            ["OwnerId"] = UserId.ToString(),
            ["Name"] = "Bia",
            ["Email"] = "bia@example.com",
            ["Phone"] = "11987654321"
        }, result.Data!.Payload);
    }

    [Fact]
    public void ToAuditLog_WhenOccurredAtHasOffset_StoresUtcTimestamp()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new UserCreated
        {
            EventId = EventId,
            OccurredAt = new DateTimeOffset(2026, 11, 2, 7, 30, 0, TimeSpan.FromHours(-3)),
            UserId = UserId,
            Name = "Ana",
            Email = "ana@example.com"
        });

        // Assert
        Assert.Equal(OccurredAt, result.Data!.Timestamp);
        Assert.Equal(DateTimeKind.Utc, result.Data.Timestamp.Kind);
    }

    [Fact]
    public void ToAuditLog_WhenUserIdIsEmpty_ReturnsFailure()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new UserDeleted { EventId = EventId, OccurredAt = OccurredAt, UserId = Guid.Empty, Name = "Ana", Email = "ana@example.com" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("UserId", result.Error);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(null)]
    public void ToAuditLog_WhenNameIsBlank_ReturnsFailure(string? name)
    {
        // Act
        var userResult = IntegrationEventMapper.ToAuditLog(new UserCreated { EventId = EventId, OccurredAt = OccurredAt, UserId = UserId, Name = name!, Email = "ana@example.com" });
        var contactResult = IntegrationEventMapper.ToAuditLog(new ContactCreated { EventId = EventId, OccurredAt = OccurredAt, ContactId = ContactId, OwnerId = UserId, Name = name! });

        // Assert
        Assert.False(userResult.IsSuccess);
        Assert.False(contactResult.IsSuccess);
    }

    [Fact]
    public void ToAuditLog_ContactCreated_MapsToContactCreated()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new ContactCreated { EventId = EventId, OccurredAt = OccurredAt, ContactId = ContactId, OwnerId = UserId, Name = "Bia" });

        // Assert
        Assert.Equal(EEventType.ContactCreated, result.Data!.EventType);
    }

    [Fact]
    public void ToAuditLog_ContactDeleted_MapsToContactDeleted()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new ContactDeleted { EventId = EventId, OccurredAt = OccurredAt, ContactId = ContactId, OwnerId = UserId, Name = "Bia" });

        // Assert
        Assert.Equal(EEventType.ContactDeleted, result.Data!.EventType);
    }

    [Fact]
    public void ToAuditLog_WhenEventIdIsEmpty_ReturnsFailure()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new UserCreated { EventId = Guid.Empty, OccurredAt = OccurredAt, UserId = UserId, Name = "Ana", Email = "ana@example.com" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("EventId", result.Error);
    }

    [Fact]
    public void ToAuditLog_WhenAggregateIdIsEmpty_ReturnsFailure()
    {
        // Act
        var result = IntegrationEventMapper.ToAuditLog(new ContactCreated { EventId = EventId, OccurredAt = OccurredAt, ContactId = Guid.Empty, OwnerId = UserId, Name = "Bia" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("ContactId", result.Error);
    }

    [Fact]
    public void ToAuditLog_UserCreatedWithPerformedBy_RecordsActor()
    {
        // Arrange
        var actor = Guid.NewGuid();

        // Act
        var result = IntegrationEventMapper.ToAuditLog(new UserCreated { EventId = EventId, OccurredAt = OccurredAt, UserId = UserId, Name = "Ana", Email = "ana@example.com", PerformedBy = actor });

        // Assert
        Assert.Equal(actor.ToString(), result.Data!.PerformedBy);
    }

    [Fact]
    public void ToAuditLog_ContactDeletedWithPerformedBy_RecordsActor()
    {
        // Arrange
        var actor = Guid.NewGuid();

        // Act
        var result = IntegrationEventMapper.ToAuditLog(new ContactDeleted { EventId = EventId, OccurredAt = OccurredAt, ContactId = ContactId, OwnerId = UserId, Name = "Bruno", PerformedBy = actor });

        // Assert
        Assert.Equal(actor.ToString(), result.Data!.PerformedBy);
    }

    [Fact]
    public void ToAuditLog_MessageWithoutPerformedBy_RecordsSystem()
    {
        // Arrange
        var oldMessage = $$"""{"eventId":"{{EventId}}","occurredAt":"2026-11-02T10:30:00+00:00","contactId":"{{ContactId}}","ownerId":"{{UserId}}","name":"Bruno"}""";
        var message = JsonSerializer.Deserialize<ContactCreated>(oldMessage, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        // Act
        var result = IntegrationEventMapper.ToAuditLog(message);

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("system", result.Data!.PerformedBy);
    }
}
