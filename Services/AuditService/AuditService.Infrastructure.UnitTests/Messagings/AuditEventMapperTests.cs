using System.Text.Json;
using Audit.Contracts;
using AuditService.Domain.Enums;
using AuditService.Infrastructure.Messagings;

namespace AuditService.Infrastructure.UnitTests.Messagings;

public sealed class AuditEventMapperTests
{
    private static readonly Guid EventId = Guid.Parse("6f1c2a9e-3b4d-4e5f-8a7b-9c0d1e2f3a4b");

    [Fact]
    public void ToAuditLog_WhenMessageIsValid_ReturnsAuditLog()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage());

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(EventId, result.Data!.EventId);
        Assert.Equal(EEventType.UserCreated, result.Data.EventType);
        Assert.Equal("Ana", result.Data.Payload!["UserName"]);
    }

    [Fact]
    public void ToAuditLog_WhenPayloadIsMissing_ReturnsAuditLogWithoutPayload()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { Payload = null });

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        Assert.Null(result.Data!.Payload);
    }

    [Fact]
    public void ToAuditLog_WhenPayloadIsNotAnObject_ReturnsFailure()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { Payload = JsonSerializer.SerializeToElement(new[] { 1, 2 }) });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Payload", result.Error);
    }

    [Fact]
    public void ToAuditLog_WhenEventIdIsEmpty_ReturnsFailure()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { EventId = Guid.Empty });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("EventId", result.Error);
    }

    [Fact]
    public void ToAuditLog_WhenEventTypeIsUnknown_ReturnsFailure()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { EventType = "user.archived" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("user.archived", result.Error);
    }

    [Theory]
    [InlineData("contact.created", EEventType.ContactCreated)]
    [InlineData("contact.updated", EEventType.ContactUpdated)]
    [InlineData("contact.deleted", EEventType.ContactDeleted)]
    public void ToAuditLog_WhenEventTypeIsContactEvent_MapsToEventType(string eventType, EEventType expected)
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { EventType = eventType, AggregateType = "Contact" });

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(expected, result.Data!.EventType);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(null)]
    public void ToAuditLog_WhenAggregateIdIsBlank_ReturnsFailure(string? aggregateId)
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { AggregateId = aggregateId! });

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ToAuditLog_WhenAggregateTypeIsBlank_ReturnsFailure()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { AggregateType = " " });

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ToAuditLog_WhenPerformedByIsBlank_ReturnsFailure()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { PerformedBy = " " });

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ToAuditLog_WhenEventTypeIsNull_ReturnsFailure()
    {
        // Act
        var result = AuditEventMapper.ToAuditLog(ValidMessage() with { EventType = null! });

        // Assert
        Assert.False(result.IsSuccess);
    }

    private static AuditEventOccurred ValidMessage() => new()
    {
        EventId = EventId,
        EventType = "user.created",
        AggregateId = Guid.NewGuid().ToString(),
        AggregateType = "User",
        PerformedBy = "system",
        PerformedAt = DateTime.UtcNow,
        Payload = JsonSerializer.SerializeToElement(new { UserName = "Ana", UserEmail = "ana@example.com" })
    };
}
