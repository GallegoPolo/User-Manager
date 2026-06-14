using AuditService.Domain.Entities;
using AuditService.Domain.Enums;
using AuditService.Domain.Interfaces.Repositories;
using AuditService.Infrastructure.Persistences;
using AuditService.Infrastructure.Resiliences;
using MongoDB.Driver;

namespace AuditService.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly MongoDbContext _context;
        private readonly IMongoCollection<AuditLog> _collection;

        public AuditLogRepository(MongoDbContext context)
        {
            _context = context;
            _collection = _context.AuditLogs;
        }

        public async Task<bool> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            try
            {
                var exists = await EventExistsAsync(auditLog.EventId, cancellationToken);
                if (exists)
                    return false;

                await _collection.InsertOneAsync(auditLog, cancellationToken: cancellationToken);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<AuditLog>.Filter.Eq(x => x.EventId, eventId);
            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            return count > 0;
        }

        public async Task<AuditLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<AuditLog>.Filter.Eq(x => x.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<(List<AuditLog> Logs, long TotalCount)> GetAllAsync(string? aggregateId = null,
                                                                              EEventType? eventType = null,
                                                                              DateTime? fromDate = null,
                                                                              DateTime? toDate = null,
                                                                              int pageNumber = 1,
                                                                              int pageSize = 20,
                                                                              CancellationToken cancellationToken = default)
        {
            var filterBuilder = Builders<AuditLog>.Filter;
            var filters = new List<FilterDefinition<AuditLog>>();
            var readPolicy = RetryPolicies.CreateMongoReadPolicy();

            return await readPolicy.ExecuteAsync(async () =>
            {
                // 1. Construção dinâmica dos filtros
                if (!string.IsNullOrWhiteSpace(aggregateId))
                    filters.Add(filterBuilder.Eq(x => x.AggregateId, aggregateId));

                if (eventType.HasValue)
                    filters.Add(filterBuilder.Eq(x => x.EventType, eventType.Value));

                if (fromDate.HasValue)
                    filters.Add(filterBuilder.Gte(x => x.Timestamp, fromDate.Value));

                if (toDate.HasValue)
                    filters.Add(filterBuilder.Lte(x => x.Timestamp, toDate.Value));

                var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

                // 2. Busca o total de documentos para a paginação
                var totalCount = await _collection.CountDocumentsAsync(finalFilter, cancellationToken: cancellationToken);

                // 3. Busca os logs paginados e ordenados
                var logs = await _collection.Find(finalFilter)
                                             .SortByDescending(x => x.Timestamp)
                                             .Skip((pageNumber - 1) * pageSize)
                                             .Limit(pageSize)
                                             .ToListAsync(cancellationToken);

                // 4. Retorna a tupla com os dados e o contador
                return (Logs: logs, TotalCount: totalCount);
            });
        }
    }
}
