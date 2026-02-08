namespace AuditService.Infrastructure.Persistences
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string AuditLogsCollectionName { get; set; } = "audit_logs";
    }
}
