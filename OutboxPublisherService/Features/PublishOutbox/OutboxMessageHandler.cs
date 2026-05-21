using LoanApplication.Domain;
using Microsoft.Extensions.Logging;

namespace OutboxPublisherService.Features.PublishOutbox;

public sealed class OutboxMessageHandler(
    LoanContext context,
    ILogger<OutboxMessageHandler> logger,
    TimeProvider timeProvider) : IOutboxMessageHandler
{
    public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Publishing outbox message {MessageId} of type {MessageType}: {Payload}",
            message.Id, message.Type, message.Payload);

        var published = message with { PublishedAt = timeProvider.GetUtcNow().UtcDateTime };
        context.OutboxMessages.Update(published);

        return Task.CompletedTask;
    }
}
