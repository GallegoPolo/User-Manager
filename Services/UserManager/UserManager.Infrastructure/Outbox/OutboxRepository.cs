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

        public Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
        {
            return _context.Set<OutboxMessage>()
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
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
