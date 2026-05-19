using EligibilityService.Features.LoanEligibility.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace EligibilityService.Features.LoanEligibility;

public static class LoanEligibilityServiceCollectionExtensions
{
    public static IServiceCollection AddLoanEligibility(this IServiceCollection services)
    {
        services.AddSingleton<IEligibilityRule, MinimumIncomeRule>();
        services.AddSingleton<IEligibilityRule, AmountWithinLimitRule>();
        services.AddSingleton<IEligibilityRule, TermWithinRangeRule>();
        return services;
    }
}
