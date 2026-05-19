using Microsoft.Extensions.DependencyInjection;

namespace LoanApplication.Features.ApplyForLoan;

public static class ApplyForLoanServiceCollectionExtensions
{
    public static IServiceCollection AddApplyForLoan(this IServiceCollection services)
    {
        services.AddScoped<LoanApplicationRequestValidator>();
        return services;
    }
}
