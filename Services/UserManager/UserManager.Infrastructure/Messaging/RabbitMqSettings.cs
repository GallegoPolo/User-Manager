namespace UserManager.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "dev";
    public string Password { get; set; } = "dev123";
    public string ExchangeName { get; set; } = "audit_exchange";
}