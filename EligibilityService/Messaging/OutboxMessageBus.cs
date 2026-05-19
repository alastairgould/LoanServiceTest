using System.Text.Json;
using LoanApplication.Domain;
using LoanApplication.Domain.Events;

namespace EligibilityService.Messaging;

internal sealed class OutboxMessageBus(LoanContext context, TimeProvider timeProvider) : IMessageBus
{
    public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        context.OutboxMessages.Add(new OutboxMessage(
            Id:          Guid.NewGuid(),
            Type:        @event.GetType().Name,
            Payload:     JsonSerializer.Serialize(@event, @event.GetType()),
            OccurredAt:  timeProvider.GetUtcNow().UtcDateTime,
            PublishedAt: null));
        
        return Task.CompletedTask;
    }
}
