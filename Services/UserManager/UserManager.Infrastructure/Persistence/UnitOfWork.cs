using MassTransit.EntityFrameworkCoreIntegration;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserManager.Application.Common;
using UserManager.Application.Events;
using UserManager.Domain.Events.Interfaces;
using UserManager.Domain.Interfaces;

namespace UserManager.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly UserManagerDbContext _context;
        private readonly IPublisher _publisher;

        public UnitOfWork(UserManagerDbContext context, IPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await DispatchDomainEventsAsync(cancellationToken);

            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
            {
                DiscardPendingChangesKeepingOutboxState();
                throw new UniqueConstraintViolationException(postgres.ConstraintName, exception);
            }
        }

        private void DiscardPendingChangesKeepingOutboxState()
        {
            var discarded = _context.ChangeTracker.Entries()
                .Where(entry => entry.Entity is not OutboxState)
                .ToList();

            foreach (var entry in discarded)
                entry.State = EntityState.Detached;
        }

        private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
        {
            var entitiesWithEvents = _context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            // Limpa ANTES de despachar evitando reprocessar se um SaveChanges futuro olhar pra essa mesma entidade ainda rastreada no ChangeTracker.
            foreach (var entity in entitiesWithEvents)
            {
                entity.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

                await _publisher.Publish(notification, cancellationToken);
            }
        }
    }
}
