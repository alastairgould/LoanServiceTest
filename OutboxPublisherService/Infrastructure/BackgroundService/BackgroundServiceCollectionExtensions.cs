using Microsoft.Extensions.DependencyInjection;

namespace OutboxPublisherService.Infrastructure.BackgroundService;

public static class BackgroundServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundProcessing(this IServiceCollection services)
    {
        services.AddSingleton<OutboxProcessorFactory>();
        services.AddHostedService<OutboxWorker>();
        return services;
    }
}
