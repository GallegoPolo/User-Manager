using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserManager.Application.Events.Interfaces;
using UserManager.Infrastructure.IntegrationTests.TestDoubles;
using UserManager.Infrastructure.Outbox;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.IntegrationTests.Outbox;

public sealed class OutboxProcessorBackgroundServiceTests
{
    private static readonly TimeSpan FirstCycleTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task ExecuteAsync_WhenDatabaseIsUnavailable_KeepsRunning()
    {
        // Arrange
        await using var services = BuildServicesWithUnreachableDatabase();
        var logger = new RecordingLogger<OutboxProcessorBackgroundService>();
        var worker = new OutboxProcessorBackgroundService(services.GetRequiredService<IServiceScopeFactory>(), logger);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await WaitUntilFirstCycleFailsAsync(worker, logger);

        // Assert
        Assert.False(worker.ExecuteTask!.IsCompleted);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_AfterDatabaseFailure_StopsWithoutFault()
    {
        // Arrange
        await using var services = BuildServicesWithUnreachableDatabase();
        var logger = new RecordingLogger<OutboxProcessorBackgroundService>();
        var worker = new OutboxProcessorBackgroundService(services.GetRequiredService<IServiceScopeFactory>(), logger);

        await worker.StartAsync(CancellationToken.None);
        await WaitUntilFirstCycleFailsAsync(worker, logger);

        // Act
        await worker.StopAsync(CancellationToken.None);

        // Assert
        Assert.False(worker.ExecuteTask!.IsFaulted);
    }

    private static ServiceProvider BuildServicesWithUnreachableDatabase()
    {
        var connectionString = $"Host=127.0.0.1;Port={GetUnusedPort()};Database=outbox;Username=test;Password=test;Timeout=2";

        return new ServiceCollection()
            .AddDbContext<UserManagerDbContext>(options => options.UseNpgsql(connectionString))
            .AddScoped<OutboxRepository>()
            .AddScoped<IAuditEventPublisher, NeverCalledAuditEventPublisher>()
            .BuildServiceProvider();
    }

    private static int GetUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitUntilFirstCycleFailsAsync(OutboxProcessorBackgroundService worker,
                                                            RecordingLogger<OutboxProcessorBackgroundService> logger)
    {
        using var timeout = new CancellationTokenSource(FirstCycleTimeout);

        while (!logger.HasErrors && !worker.ExecuteTask!.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }

    private sealed class NeverCalledAuditEventPublisher : IAuditEventPublisher
    {
        public Task PublishAsync(Guid eventId,
                                 string eventType,
                                 string aggregateId,
                                 string aggregateType,
                                 string performedBy,
                                 DateTime occurredAt,
                                 string payloadJson,
                                 CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Sem banco não há mensagens pendentes para publicar.");
        }
    }
}
