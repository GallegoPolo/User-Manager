namespace AuditService.Infrastructure.Messagings
{
    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public ushort Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string QueueName { get; set; } = "audit_events";
        public ushort PrefetchCount { get; set; } = 10;
    }
}
