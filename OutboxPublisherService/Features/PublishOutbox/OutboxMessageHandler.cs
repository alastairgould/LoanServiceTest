using LoanApplication.Domain;
using Microsoft.Extensions.Logging;

namespace OutboxPublisherService.Features.PublishOutbox;

public class OutboxMessageHandler(
    LoanContext context,
    ILogger<OutboxMessageHandler> logger,
    TimeProvider timeProvider)
{
    public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Publishing outbox message {MessageId} of type {MessageType}: {Payload}",
            message.Id, message.Type, message.Payload);

        context.Entry(message)
            .Property(m => m.PublishedAt)
            .CurrentValue = timeProvider.GetUtcNow().UtcDateTime;

        return Task.CompletedTask;
    }
}
