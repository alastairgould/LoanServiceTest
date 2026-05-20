using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OutboxPublisherService.Features.PublishOutbox;

namespace OutboxPublisherService.Infrastructure.BackgroundService;

public sealed class OutboxProcessorFactory(
    IDbContextFactory<LoanContext> contextFactory,
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider,
    int batchSize = 100)
{
    public async Task<OutboxProcessor> CreateAsync(CancellationToken cancellationToken = default)
    {
        var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var logger = loggerFactory.CreateLogger<OutboxMessageHandler>();
        var handler = new OutboxMessageHandler(context, logger, timeProvider);
        return new OutboxProcessor(context, handler, batchSize);
    }
}
