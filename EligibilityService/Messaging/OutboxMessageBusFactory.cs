using LoanApplication.Domain;

namespace EligibilityService.Messaging;

public sealed class OutboxMessageBusFactory(TimeProvider timeProvider) : IMessageBusFactory
{
    public IMessageBus CreateFor(LoanContext context) => new OutboxMessageBus(context, timeProvider);
}
