using AuditService.Infrastructure.Messagings;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace AuditService.Infrastructure.IntegrationTests.Messagings;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private const string UserName = "test";
    private const string Password = "test";

    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:3-management")
        .WithUsername(UserName)
        .WithPassword(Password)
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public RabbitMqSettings CreateSettings(string queueName) => new()
    {
        HostName = _container.Hostname,
        Port = _container.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort),
        UserName = UserName,
        Password = Password,
        QueueName = queueName
    };

    public Task<IConnection> CreateConnectionAsync()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_container.GetConnectionString()) };
        return factory.CreateConnectionAsync();
    }
}
