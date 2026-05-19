using LoanApplication.Domain;

namespace EligibilityService.Messaging;

public interface IEventPublisherFactory
{
    IEventPublisher CreateFor(LoanContext context);
}
