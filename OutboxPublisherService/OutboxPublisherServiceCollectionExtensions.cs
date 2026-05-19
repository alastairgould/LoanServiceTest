using Microsoft.Extensions.DependencyInjection;
using OutboxPublisherService.Features.PublishOutbox;
using OutboxPublisherService.Infrastructure.BackgroundService;

namespace OutboxPublisherService;

public static class OutboxPublisherServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxPublisherService(this IServiceCollection services)
    {
        services.AddPublishOutbox();
        services.AddBackgroundProcessing();
        return services;
    }
}
