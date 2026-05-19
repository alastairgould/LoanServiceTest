using Microsoft.Extensions.DependencyInjection;

namespace EligibilityService.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisherFactory, OutboxEventPublisherFactory>();
        return services;
    }
}
