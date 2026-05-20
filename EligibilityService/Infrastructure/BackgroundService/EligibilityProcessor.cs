using EligibilityService.Features.LoanEligibility;
using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;

namespace EligibilityService.Infrastructure.BackgroundService;

public sealed class EligibilityProcessor(
    LoanContext context,
    LoanEligibilityHandler handler,
    int batchSize) : IAsyncDisposable
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var pending = await context.LoanApplications
            .AsNoTracking()
            .Where(la => la.Status == LoanStatus.Pending)
            .OrderBy(la => la.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var loan in pending)
        {
            await handler.HandleAsync(loan, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
