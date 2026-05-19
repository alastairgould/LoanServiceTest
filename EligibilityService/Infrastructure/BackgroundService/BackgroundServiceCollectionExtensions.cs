using Microsoft.Extensions.DependencyInjection;

namespace EligibilityService.Infrastructure.BackgroundService;

public static class BackgroundServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundProcessing(this IServiceCollection services)
    {
        services.AddSingleton<EligibilityProcessorFactory>();
        services.AddHostedService<EligibilityWorker>();
        return services;
    }
}
