using LoanEntity = LoanApplication.Domain.LoanApplication;

namespace EligibilityService.Features.LoanEligibility.Rules;

public class MinimumIncomeRule : IEligibilityRule
{
    public string Name => "MinimumIncome";
    public string Message => "Monthly income must be at least £2,000";
    public bool Evaluate(LoanEntity loan) => loan.MonthlyIncome >= 2000;
}
