using MediatR;
using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.Outbox.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Contacts.EventHandlers;

public sealed class ContactUpdatedAuditHandler : INotificationHandler<DomainEventNotification<ContactUpdatedDomainEvent>>
{
    private readonly IOutboxRepository _outboxRepository;

    public ContactUpdatedAuditHandler(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }

    public Task Handle(DomainEventNotification<ContactUpdatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var payload = new ContactAuditPayload(domainEvent.OwnerId, domainEvent.Name, domainEvent.Email, domainEvent.Phone);

        _outboxRepository.Add(eventType: "contact.updated",
                              aggregateId: domainEvent.ContactId.ToString(),
                              aggregateType: "Contact",
                              payloadJson: JsonSerializer.Serialize(payload));

        return Task.CompletedTask;
    }
}
