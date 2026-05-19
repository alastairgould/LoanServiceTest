using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;
using OutboxPublisherService.Features.PublishOutbox;

namespace OutboxPublisherService.Infrastructure.BackgroundService;

public sealed class OutboxProcessor(
    LoanContext context,
    OutboxMessageHandler handler) : IAsyncDisposable
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var unpublished = await context.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.OccurredAt)
            .ToListAsync(cancellationToken);

        foreach (var message in unpublished)
        {
            await handler.HandleAsync(message, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
