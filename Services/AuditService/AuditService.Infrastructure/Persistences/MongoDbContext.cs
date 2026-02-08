using AuditService.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AuditService.Infrastructure.Persistences
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _settings;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            _settings = settings.Value;

            var client = new MongoClient(_settings.ConnectionString);
            _database = client.GetDatabase(_settings.DatabaseName);

            CreateIndexes();
        }

        public IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>(_settings.AuditLogsCollectionName);

        private void CreateIndexes()
        {
            var eventIdIndexKeys = Builders<AuditLog>.IndexKeys.Ascending(x => x.EventId);
            var eventIdIndexOptions = new CreateIndexOptions { Unique = true };
            var eventIdIndexModel = new CreateIndexModel<AuditLog>(eventIdIndexKeys, eventIdIndexOptions);

            var aggregateIndexKeys = Builders<AuditLog>.IndexKeys.Ascending(x => x.AggregateId).Descending(x => x.Timestamp);
            var aggregateIndexModel = new CreateIndexModel<AuditLog>(aggregateIndexKeys);

            var timestampIndexKeys = Builders<AuditLog>.IndexKeys.Descending(x => x.Timestamp);
            var timestampIndexModel = new CreateIndexModel<AuditLog>(timestampIndexKeys);

            AuditLogs.Indexes.CreateMany(new[] { eventIdIndexModel, aggregateIndexModel, timestampIndexModel });
        }
    }
}
