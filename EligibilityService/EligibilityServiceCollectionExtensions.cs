using EligibilityService.Features.LoanEligibility;
using EligibilityService.Infrastructure.BackgroundService;
using EligibilityService.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace EligibilityService;

public static class EligibilityServiceCollectionExtensions
{
    public static IServiceCollection AddEligibilityService(this IServiceCollection services)
    {
        services.AddLoanEligibility();
        services.AddMessaging();
        services.AddBackgroundProcessing();
        return services;
    }
}
