using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using UserManager.Infrastructure.Messaging;

namespace UserManager.Infrastructure.IntegrationTests.Messaging;

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

    public RabbitMqSettings CreateSettings(string exchangeName) => new()
    {
        HostName = _container.Hostname,
        Port = _container.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort),
        UserName = UserName,
        Password = Password,
        ExchangeName = exchangeName
    };

    public Task<IConnection> CreateConnectionAsync()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_container.GetConnectionString()) };
        return factory.CreateConnectionAsync();
    }
}
