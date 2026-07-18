using MediatR;
using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.Outbox.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers
{
    public class UserCreatedAuditHandler : INotificationHandler<DomainEventNotification<UserCreatedDomainEvent>>
    {
        private readonly IOutboxRepository _outboxRepository;

        public UserCreatedAuditHandler(IOutboxRepository outboxRepository)
        {
            _outboxRepository = outboxRepository;
        }

        public Task Handle(DomainEventNotification<UserCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var payload = new UserCreatedAuditPayload(domainEvent.Name, domainEvent.Email);
            var payloadJson = JsonSerializer.Serialize(payload);

            _outboxRepository.Add(eventType: "user.created",
                                  aggregateId: domainEvent.UserId.ToString(),
                                  aggregateType: "User",
                                  payloadJson: payloadJson);

            return Task.CompletedTask;
        }
    }
}
