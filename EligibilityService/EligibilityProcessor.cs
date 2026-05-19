using EligibilityService.Messaging;
using EligibilityService.Rules;
using LoanApplication.Domain;
using LoanApplication.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EligibilityService;

public class EligibilityProcessor(
    IDbContextFactory<LoanContext> contextFactory,
    TimeProvider timeProvider,
    IMessageBusFactory busFactory,
    IEnumerable<IEligibilityRule> rules)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var bus = busFactory.CreateFor(context);

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
                var logMessage = passed ? "Passed Eligibility Rule" : message;
                context.DecisionLogEntries.Add(new DecisionLogEntry(Guid.NewGuid(), loan.Id, name, passed, logMessage, now));
            }

            var newStatus = results.All(r => r.Passed) ? LoanStatus.Approved : LoanStatus.Rejected;
            var updated = loan with { Status = newStatus, ReviewedAt = now };
            context.LoanApplications.Update(updated);

            IEvent @event = newStatus == LoanStatus.Approved
                ? new LoanApproved(loan.Id, now)
                : new LoanRejected(loan.Id, now);
            
            await bus.PublishAsync(@event, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
