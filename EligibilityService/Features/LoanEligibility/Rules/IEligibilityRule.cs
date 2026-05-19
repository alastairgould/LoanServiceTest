using LoanEntity = LoanApplication.Domain.LoanApplication;

namespace EligibilityService.Features.LoanEligibility.Rules;

public interface IEligibilityRule
{
    string Name { get; }
    string Message { get; }
    bool Evaluate(LoanEntity loan);
}
