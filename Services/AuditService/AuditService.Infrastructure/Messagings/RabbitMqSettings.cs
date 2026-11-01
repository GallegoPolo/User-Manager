namespace AuditService.Infrastructure.Messagings
{
    public class RabbitMqSettings
    {
        private static readonly TimeSpan[] DefaultRetryIntervals =
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30)
        ];

        public string HostName { get; set; } = "localhost";
        public ushort Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string QueueName { get; set; } = "audit_events";
        public ushort PrefetchCount { get; set; } = 10;
        public TimeSpan[]? RetryIntervals { get; set; }

        public TimeSpan[] GetRetryIntervals() => RetryIntervals is { Length: > 0 } ? RetryIntervals : DefaultRetryIntervals;
    }
}
