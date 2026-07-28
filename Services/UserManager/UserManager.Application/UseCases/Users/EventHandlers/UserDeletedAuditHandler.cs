using MediatR;
using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.Outbox.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers
{
    public class UserDeletedAuditHandler : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        private readonly IOutboxRepository _outboxRepository;

        public UserDeletedAuditHandler(IOutboxRepository outboxRepository)
        {
            _outboxRepository = outboxRepository;
        }

        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var payload = new UserDeletedAuditPayload(domainEvent.Name, domainEvent.Email);
            var payloadJson = JsonSerializer.Serialize(payload);

            _outboxRepository.Add(eventType: "user.deleted",
                                  aggregateId: domainEvent.UserId.ToString(),
                                  aggregateType: "User",
                                  payloadJson: payloadJson);

            return Task.CompletedTask;
        }
    }
}