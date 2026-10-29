using MediatR;
using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.Outbox.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Contacts.EventHandlers;

public sealed class ContactCreatedAuditHandler : INotificationHandler<DomainEventNotification<ContactCreatedDomainEvent>>
{
    private readonly IOutboxRepository _outboxRepository;

    public ContactCreatedAuditHandler(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }

    public Task Handle(DomainEventNotification<ContactCreatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var payload = new ContactCreatedAuditPayload(domainEvent.OwnerId, domainEvent.Name, domainEvent.Email, domainEvent.Phone);

        _outboxRepository.Add(eventType: "contact.created",
                              aggregateId: domainEvent.ContactId.ToString(),
                              aggregateType: "Contact",
                              payloadJson: JsonSerializer.Serialize(payload));

        return Task.CompletedTask;
    }
}
