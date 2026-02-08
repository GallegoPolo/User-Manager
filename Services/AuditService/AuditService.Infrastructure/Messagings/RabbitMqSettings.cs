namespace AuditService.Infrastructure.Messagings
{
    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string QueueName { get; set; } = "audit_events";
        public string ExchangeName { get; set; } = "audit_exchange";
        public string RoutingKey { get; set; } = "audit.#";
        public bool Durable { get; set; } = true;
        public ushort PrefetchCount { get; set; } = 10;
    }
}
