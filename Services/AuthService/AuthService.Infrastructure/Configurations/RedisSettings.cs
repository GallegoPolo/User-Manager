namespace AuthService.Infrastructure.Configurations
{
    public class RedisSettings
    {
        public int CacheDurationMinutes { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
    }
}
