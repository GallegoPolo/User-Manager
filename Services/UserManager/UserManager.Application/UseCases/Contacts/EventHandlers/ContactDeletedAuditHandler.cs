using MediatR;
using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.Outbox.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Contacts.EventHandlers;

public sealed class ContactDeletedAuditHandler : INotificationHandler<DomainEventNotification<ContactDeletedDomainEvent>>
{
    private readonly IOutboxRepository _outboxRepository;

    public ContactDeletedAuditHandler(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }

    public Task Handle(DomainEventNotification<ContactDeletedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var payload = new ContactAuditPayload(domainEvent.OwnerId, domainEvent.Name, domainEvent.Email, domainEvent.Phone);

        _outboxRepository.Add(eventType: "contact.deleted",
                              aggregateId: domainEvent.ContactId.ToString(),
                              aggregateType: "Contact",
                              payloadJson: JsonSerializer.Serialize(payload));

        return Task.CompletedTask;
    }
}
