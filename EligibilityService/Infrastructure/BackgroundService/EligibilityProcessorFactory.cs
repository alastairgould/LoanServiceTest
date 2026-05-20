using EligibilityService.Features.LoanEligibility;
using EligibilityService.Features.LoanEligibility.Rules;
using EligibilityService.Infrastructure.EventPublishing;
using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EligibilityService.Infrastructure.BackgroundService;

public sealed class EligibilityProcessorFactory(
    IDbContextFactory<LoanContext> contextFactory,
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider,
    IEnumerable<IEligibilityRule> rules,
    int batchSize = 100)
{
    public async Task<EligibilityProcessor> CreateAsync(CancellationToken cancellationToken = default)
    {
        var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var publisher = new OutboxEventPublisher(context, timeProvider);
        var handler = new LoanEligibilityHandler(timeProvider, rules, context, publisher);
        var logger = loggerFactory.CreateLogger<EligibilityProcessor>();
        return new EligibilityProcessor(context, handler, logger, batchSize);
    }
}
