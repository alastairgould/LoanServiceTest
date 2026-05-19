using LoanEntity = LoanApplication.Domain.LoanApplication;

namespace EligibilityService.Features.LoanEligibility.Rules;

public class AmountWithinLimitRule : IEligibilityRule
{
    public string Name => "AmountWithinLimit";
    public string Message => "Requested amount must be no more than monthly income multiplied by 4";
    public bool Evaluate(LoanEntity loan) => loan.RequestedAmount <= loan.MonthlyIncome * 4m;
}
