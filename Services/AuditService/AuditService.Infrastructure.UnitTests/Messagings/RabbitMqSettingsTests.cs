using AuditService.Infrastructure.Messagings;
using Microsoft.Extensions.Configuration;

namespace AuditService.Infrastructure.UnitTests.Messagings;

public sealed class RabbitMqSettingsTests
{
    [Fact]
    public void GetRetryIntervals_WhenConfigured_ReturnsOnlyConfiguredIntervals()
    {
        // Arrange
        var settings = Bind(new Dictionary<string, string?>
        {
            ["RabbitMq:RetryIntervals:0"] = "00:00:02",
            ["RabbitMq:RetryIntervals:1"] = "00:00:04"
        });

        // Act
        var intervals = settings.GetRetryIntervals();

        // Assert
        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)], intervals);
    }

    [Fact]
    public void GetRetryIntervals_WhenNotConfigured_ReturnsDefaultIntervals()
    {
        // Arrange
        var settings = Bind(new Dictionary<string, string?> { ["RabbitMq:QueueName"] = "audit_events" });

        // Act
        var intervals = settings.GetRetryIntervals();

        // Assert
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)], intervals);
    }

    private static RabbitMqSettings Bind(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()!;
    }
}
