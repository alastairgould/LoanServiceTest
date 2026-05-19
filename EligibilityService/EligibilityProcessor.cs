using EligibilityService.Rules;
using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;

namespace EligibilityService;

public class EligibilityProcessor(
    IDbContextFactory<LoanContext> contextFactory,
    TimeProvider timeProvider,
    IEnumerable<IEligibilityRule> rules)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var pending = await context.LoanApplications
            .AsNoTracking()
            .Where(la => la.Status == LoanStatus.Pending)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var loan in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var results = rules
                .Select(rule => (rule.Name, Passed: rule.Evaluate(loan), rule.Message))
                .ToList();

            foreach (var (name, passed, message) in results)
            {
                context.DecisionLogEntries.Add(new DecisionLogEntry(Guid.NewGuid(), loan.Id, name, passed, message, now));
            }

            var newStatus = results.All(r => r.Passed) ? LoanStatus.Approved : LoanStatus.Rejected;
            var updated = loan with { Status = newStatus, ReviewedAt = now };
            context.LoanApplications.Update(updated);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
