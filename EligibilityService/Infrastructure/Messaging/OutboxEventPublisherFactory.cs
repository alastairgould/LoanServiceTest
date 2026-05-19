using LoanApplication.Domain;

namespace EligibilityService.Infrastructure.Messaging;

public sealed class OutboxEventPublisherFactory(TimeProvider timeProvider) : IEventPublisherFactory
{
    public IEventPublisher CreateFor(LoanContext context) => new OutboxEventPublisher(context, timeProvider);
}
