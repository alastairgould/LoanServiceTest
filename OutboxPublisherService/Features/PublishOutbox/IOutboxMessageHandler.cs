using LoanApplication.Domain;

namespace OutboxPublisherService.Features.PublishOutbox;

public interface IOutboxMessageHandler
{
    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken);
}
