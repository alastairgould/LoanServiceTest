using LoanApplication.Domain.Events;

namespace EligibilityService.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
