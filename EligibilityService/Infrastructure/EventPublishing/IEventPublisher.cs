using LoanApplication.Domain.Events;

namespace EligibilityService.Infrastructure.EventPublishing;

public interface IEventPublisher
{
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
