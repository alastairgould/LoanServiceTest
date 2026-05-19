using LoanEntity = LoanApplication.Domain.LoanApplication;

namespace EligibilityService.Features.LoanEligibility.Rules;

public class TermWithinRangeRule : IEligibilityRule
{
    public string Name => "TermWithinRange";
    public string Message => "Term must be between 12 and 60 months";
    public bool Evaluate(LoanEntity loan) => loan.TermMonths >= 12 && loan.TermMonths <= 60;
}
