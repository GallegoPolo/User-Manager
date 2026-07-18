using MediatR;
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
            return await _context.SaveChangesAsync(cancellationToken);
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
