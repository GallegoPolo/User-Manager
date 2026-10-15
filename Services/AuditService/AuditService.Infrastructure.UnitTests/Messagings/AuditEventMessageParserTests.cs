using System.Text;
using System.Text.Json.Nodes;
using AuditService.Domain.Enums;
using AuditService.Infrastructure.Messagings;

namespace AuditService.Infrastructure.UnitTests.Messagings;

public sealed class AuditEventMessageParserTests
{
    private static readonly Guid EventId = Guid.Parse("6f1c2a9e-3b4d-4e5f-8a7b-9c0d1e2f3a4b");

    [Fact]
    public void Parse_WhenMessageIsValid_ReturnsAuditLog()
    {
        // Arrange
        var body = ToBody(ValidMessage());

        // Act
        var result = AuditEventMessageParser.Parse(body);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EventId, result.Data!.EventId);
        Assert.Equal(EEventType.UserCreated, result.Data.EventType);
        Assert.Equal("Ana", result.Data.Payload!["UserName"]);
    }

    [Fact]
    public void Parse_WhenJsonIsMalformed_ReturnsFailure()
    {
        // Arrange
        var body = Encoding.UTF8.GetBytes("""{"eventId": """);

        // Act
        var result = AuditEventMessageParser.Parse(body);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.StartsWith("JSON inválido", result.Error);
    }

    [Fact]
    public void Parse_WhenBodyIsNullLiteral_ReturnsFailure()
    {
        // Arrange
        var body = Encoding.UTF8.GetBytes("null");

        // Act
        var result = AuditEventMessageParser.Parse(body);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Parse_WhenEventIdIsMissing_ReturnsFailure()
    {
        // Arrange
        var message = ValidMessage();
        message.Remove("eventId");

        // Act
        var result = AuditEventMessageParser.Parse(ToBody(message));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("eventId", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WhenEventIdIsEmpty_ReturnsFailure()
    {
        // Arrange
        var message = ValidMessage();
        message["eventId"] = Guid.Empty;

        // Act
        var result = AuditEventMessageParser.Parse(ToBody(message));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Parse_WhenEventTypeIsUnknown_ReturnsFailure()
    {
        // Arrange
        var message = ValidMessage();
        message["eventType"] = "user.archived";

        // Act
        var result = AuditEventMessageParser.Parse(ToBody(message));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("user.archived", result.Error);
    }

    [Theory]
    [InlineData("aggregateId")]
    [InlineData("aggregateType")]
    [InlineData("performedBy")]
    public void Parse_WhenRequiredFieldIsBlank_ReturnsFailure(string field)
    {
        // Arrange
        var message = ValidMessage();
        message[field] = " ";

        // Act
        var result = AuditEventMessageParser.Parse(ToBody(message));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("eventType")]
    [InlineData("aggregateId")]
    public void Parse_WhenNonNullableFieldIsNull_ReturnsFailure(string field)
    {
        // Arrange
        var message = ValidMessage();
        message[field] = null;

        // Act
        var result = AuditEventMessageParser.Parse(ToBody(message));

        // Assert
        Assert.False(result.IsSuccess);
    }

    private static JsonObject ValidMessage() => new()
    {
        ["eventId"] = EventId,
        ["eventType"] = "user.created",
        ["aggregateId"] = Guid.NewGuid().ToString(),
        ["aggregateType"] = "User",
        ["performedBy"] = "system",
        ["performedAt"] = DateTime.UtcNow,
        ["payload"] = new JsonObject { ["UserName"] = "Ana", ["UserEmail"] = "ana@example.com" }
    };

    private static byte[] ToBody(JsonObject message) => Encoding.UTF8.GetBytes(message.ToJsonString());
}
