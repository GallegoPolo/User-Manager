using MediatR;
using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.Outbox.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers
{
    public class UserUpdatedAuditHandler : INotificationHandler<DomainEventNotification<UserUpdatedDomainEvent>>
    {
        private readonly IOutboxRepository _outboxRepository;

        public UserUpdatedAuditHandler(IOutboxRepository outboxRepository)
        {
            _outboxRepository = outboxRepository;
        }

        public Task Handle(DomainEventNotification<UserUpdatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var payload = new UserUpdatedAuditPayload(domainEvent.Name, domainEvent.Email);
            var payloadJson = JsonSerializer.Serialize(payload);

            _outboxRepository.Add(eventType: "user.updated",
                                  aggregateId: domainEvent.UserId.ToString(),
                                  aggregateType: "User",
                                  payloadJson: payloadJson);

            return Task.CompletedTask;
        }
    }
}