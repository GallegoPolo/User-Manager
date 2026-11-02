using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Audit.Contracts;
using AuditService.Domain.Interfaces.Repositories;
using AuditService.Infrastructure.IntegrationTests.TestDoubles;
using AuditService.Infrastructure.Messagings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.RabbitMq;

namespace AuditService.Infrastructure.IntegrationTests.Messagings;

public sealed class AuditEventConsumerStartupTests
{
    private const string UserName = "test";
    private const string Password = "test";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(90);

    [Fact]
    public async Task Consumer_StartedBeforeRabbitMq_ConsumesOnceBrokerIsUp()
    {
        // Arrange
        var port = FreeTcpPort();
        var repository = new FakeAuditLogRepository(failuresBeforeSuccess: 0);
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IAuditLogRepository>(repository)
            .AddAuditConsumers(new RabbitMqSettings
            {
                HostName = "localhost",
                Port = (ushort)port,
                UserName = UserName,
                Password = Password,
                QueueName = $"audit_events_{Guid.NewGuid():N}"
            })
            .BuildServiceProvider();

        var hostedServices = services.GetServices<IHostedService>().ToList();
        foreach (var hostedService in hostedServices)
            await hostedService.StartAsync(CancellationToken.None);

        await using var rabbitMq = new RabbitMqBuilder("rabbitmq:3-management")
            .WithUsername(UserName)
            .WithPassword(Password)
            .WithPortBinding(port, RabbitMqBuilder.RabbitMqPort)
            .Build();

        try
        {
            // Act
            await rabbitMq.StartAsync();
            await services.GetRequiredService<IBusControl>().WaitForHealthStatus(BusHealthStatus.Healthy, WaitTimeout);
            await services.GetRequiredService<IBus>().Publish(SampleMessage());

            // Assert
            await repository.WaitForSavedAsync(WaitTimeout);
        }
        finally
        {
            foreach (var hostedService in Enumerable.Reverse(hostedServices))
                await hostedService.StopAsync(CancellationToken.None);
        }
    }

    private static int FreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static AuditEventOccurred SampleMessage() => new()
    {
        EventId = Guid.NewGuid(),
        EventType = "user.created",
        AggregateId = Guid.NewGuid().ToString(),
        AggregateType = "User",
        PerformedBy = "system",
        PerformedAt = DateTime.UtcNow,
        Payload = JsonSerializer.SerializeToElement(new { UserName = "Ana", UserEmail = "ana@example.com" })
    };
}
