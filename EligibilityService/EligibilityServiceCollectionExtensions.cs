using EligibilityService.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace EligibilityService;

public static class EligibilityServiceCollectionExtensions
{
    public static IServiceCollection AddEligibilityService(this IServiceCollection services)
    {
        services.AddSingleton<EligibilityProcessor>();
        services.AddSingleton<IEligibilityRule, MinimumIncomeRule>();
        services.AddSingleton<IEligibilityRule, AmountWithinLimitRule>();
        services.AddSingleton<IEligibilityRule, TermWithinRangeRule>();
        services.AddHostedService<EligibilityWorker>();
        return services;
    }
}
