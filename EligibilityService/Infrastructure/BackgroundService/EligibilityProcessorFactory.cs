using EligibilityService.Features.LoanEligibility;
using EligibilityService.Features.LoanEligibility.Rules;
using EligibilityService.Infrastructure.Messaging;
using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;

namespace EligibilityService.Infrastructure.BackgroundService;

public sealed class EligibilityProcessorFactory(
    IDbContextFactory<LoanContext> contextFactory,
    IEventPublisherFactory publisherFactory,
    TimeProvider timeProvider,
    IEnumerable<IEligibilityRule> rules)
{
    public async Task<EligibilityProcessor> CreateAsync(CancellationToken cancellationToken = default)
    {
        var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var publisher = publisherFactory.CreateFor(context);
        var handler = new LoanEligibilityHandler(timeProvider, rules, context, publisher);
        return new EligibilityProcessor(context, handler);
    }
}
