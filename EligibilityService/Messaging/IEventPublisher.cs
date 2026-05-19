using LoanApplication.Domain.Events;

namespace EligibilityService.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
