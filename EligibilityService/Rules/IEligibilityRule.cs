using LoanEntity = LoanApplication.Domain.LoanApplication;

namespace EligibilityService.Rules;

public interface IEligibilityRule
{
    string Name { get; }
    string Message { get; }
    bool Evaluate(LoanEntity loan);
}
