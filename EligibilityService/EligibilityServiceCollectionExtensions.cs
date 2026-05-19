using Microsoft.Extensions.DependencyInjection;

namespace EligibilityService;

public static class EligibilityServiceCollectionExtensions
{
    public static IServiceCollection AddEligibilityService(this IServiceCollection services)
    {
        services.AddHostedService<EligibilityWorker>();
        return services;
    }
}
