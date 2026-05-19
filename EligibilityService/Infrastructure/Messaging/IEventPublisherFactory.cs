using LoanApplication.Domain;

namespace EligibilityService.Infrastructure.Messaging;

public interface IEventPublisherFactory
{
    IEventPublisher CreateFor(LoanContext context);
}
