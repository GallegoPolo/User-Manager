using UserManager.Application.Outbox.Interfaces;
using UserManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UserManager.Infrastructure.Outbox
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly UserManagerDbContext _context;

        public OutboxRepository(UserManagerDbContext context)
        {
            _context = context;
        }

        public void Add(string eventType, string aggregateId, string aggregateType, string payloadJson)
        {
            var message = OutboxMessage.Create(eventType, aggregateId, aggregateType, payloadJson);
            _context.Set<OutboxMessage>().Add(message); 
        }

        public async Task<List<OutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan leaseDuration, CancellationToken cancellationToken)
        {
            var claimed = await _context.Set<OutboxMessage>()
                .FromSql($"""
                    UPDATE "OutboxMessages"
                    SET "LockedUntil" = now() + {leaseDuration}
                    WHERE "Id" IN (
                        SELECT "Id"
                        FROM "OutboxMessages"
                        WHERE "ProcessedAt" IS NULL
                          AND ("LockedUntil" IS NULL OR "LockedUntil" < now())
                        ORDER BY "OccurredAt"
                        LIMIT {batchSize}
                        FOR UPDATE SKIP LOCKED)
                    RETURNING *
                    """)
                .ToListAsync(cancellationToken);

            return claimed.OrderBy(m => m.OccurredAt).ToList();
        }

        public async Task MarkAsProcessedAsync(Guid outboxMessageId, CancellationToken cancellationToken)
        {
            var message = await _context.Set<OutboxMessage>().FindAsync(new object[] { outboxMessageId }, cancellationToken);
            if (message is not null)
            {
                message.MarkAsProcessed();
                await _context.SaveChangesAsync(cancellationToken); 
            }
        }
    }
}
