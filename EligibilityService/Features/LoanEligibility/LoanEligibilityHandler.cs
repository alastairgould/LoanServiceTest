using EligibilityService.Features.LoanEligibility.Rules;
using EligibilityService.Infrastructure.Messaging;
using LoanApplication.Domain;
using LoanApplication.Domain.Events;
using LoanEntity = LoanApplication.Domain.LoanApplication;

namespace EligibilityService.Features.LoanEligibility;

public class LoanEligibilityHandler(
    TimeProvider timeProvider,
    IEnumerable<IEligibilityRule> rules,
    LoanContext context,
    IEventPublisher publisher)
{
    public async Task HandleAsync(LoanEntity loan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow().UtcDateTime;

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

        await publisher.PublishAsync(@event, cancellationToken);
    }
}
