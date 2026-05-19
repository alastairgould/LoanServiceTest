using LoanApplication.Domain.Events;

namespace EligibilityService.Messaging;

public interface IMessageBus
{
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
