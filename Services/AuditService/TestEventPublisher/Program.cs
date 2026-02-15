using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

Console.WriteLine("=== Publicador de Eventos de Teste ===\n");

var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "dev",
    Password = "dev123"
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(
    exchange: "audit_exchange",
    type: ExchangeType.Topic,
    durable: true);

var testEvent = new
{
    eventId = Guid.NewGuid(),
    eventType = "user.created",
    aggregateId = Guid.NewGuid().ToString(),
    aggregateType = "User",
    performedBy = "test-admin-id",
    performedAt = DateTime.UtcNow,
    payload = new Dictionary<string, object>
    {
        { "changedFields", new[] { "name", "email" } },
        { "userName", "João Gallego" },
        { "userEmail", "joao@example.com" }
    }
};

var messageJson = JsonSerializer.Serialize(testEvent);
var messageBody = Encoding.UTF8.GetBytes(messageJson);

await channel.BasicPublishAsync(
    exchange: "audit_exchange",
    routingKey: "audit.user.created",
    body: messageBody);

Console.WriteLine($"✅ Evento publicado com sucesso!");
Console.WriteLine($"EventId: {testEvent.eventId}");
Console.WriteLine($"EventType: {testEvent.eventType}");
Console.WriteLine($"AggregateId: {testEvent.aggregateId}");
Console.WriteLine($"\n📋 JSON do evento:");
Console.WriteLine(messageJson);
Console.WriteLine($"\n🔍 Agora consulte a API em: https://localhost:7175/api/audit/logs");