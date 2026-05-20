using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OutboxPublisherService.Features.PublishOutbox;

namespace OutboxPublisherService.Infrastructure.BackgroundService;

public sealed class OutboxProcessor(
    LoanContext context,
    OutboxMessageHandler handler,
    ILogger<OutboxProcessor> logger,
    int batchSize) : IAsyncDisposable
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var unpublished = await context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in unpublished)
        {
            try
            {
                await handler.HandleAsync(message, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
            }
            finally
            {
                context.ChangeTracker.Clear();
            }
        }
    }

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
