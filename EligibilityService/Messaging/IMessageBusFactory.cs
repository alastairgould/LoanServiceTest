using LoanApplication.Domain;

namespace EligibilityService.Messaging;

public interface IMessageBusFactory
{
    IMessageBus CreateFor(LoanContext context);
}
