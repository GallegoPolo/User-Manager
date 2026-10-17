using AuditService.Infrastructure.Messagings;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace AuditService.Infrastructure.IntegrationTests.Messagings;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private const string UserName = "test";
    private const string Password = "test";
    private const string DeadLetterPolicyName = "audit-events-dlx";

    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:3-management")
        .WithUsername(UserName)
        .WithPassword(Password)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ApplyDeadLetterPolicyAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public RabbitMqSettings CreateSettings() => new()
    {
        HostName = _container.Hostname,
        Port = _container.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort),
        UserName = UserName,
        Password = Password
    };

    public Task<IConnection> CreateConnectionAsync()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_container.GetConnectionString()) };
        return factory.CreateConnectionAsync();
    }

    private async Task ApplyDeadLetterPolicyAsync()
    {
        var settings = CreateSettings();
        var definition = $$"""{"dead-letter-exchange":"{{settings.DeadLetterExchangeName}}"}""";

        var result = await _container.ExecAsync(
        [
            "rabbitmqctl", "set_policy", DeadLetterPolicyName, $"^{settings.QueueName}$", definition, "--apply-to", "queues"
        ]);

        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Falha ao aplicar a policy de dead letter: {result.Stderr}");
    }
}
