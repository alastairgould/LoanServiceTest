using EligibilityService.Features.LoanEligibility;
using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EligibilityService.Infrastructure.BackgroundService;

public sealed class EligibilityProcessor(
    LoanContext context,
    LoanEligibilityHandler handler,
    ILogger<EligibilityProcessor> logger,
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
            try
            {
                await handler.HandleAsync(loan, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process loan {LoanId}", loan.Id);
            }
            finally
            {
                context.ChangeTracker.Clear();
            }
        }
    }

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
