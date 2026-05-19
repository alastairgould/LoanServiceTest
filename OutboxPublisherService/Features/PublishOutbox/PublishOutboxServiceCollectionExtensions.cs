using Microsoft.Extensions.DependencyInjection;

namespace OutboxPublisherService.Features.PublishOutbox;

public static class PublishOutboxServiceCollectionExtensions
{
    public static IServiceCollection AddPublishOutbox(this IServiceCollection services)
    {
        return services;
    }
}
