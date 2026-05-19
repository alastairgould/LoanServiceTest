namespace LoanApplication.Features.ApplyForLoan;

public record LoanApplication(Guid Id, string Name, string Email, int MonthlyIncome, decimal RequestedAmount, int TermMonths, LoanStatus Status, DateTime CreatedAt, DateTime? ReviewedAt);

public record DecisionLogEntry(Guid Id, Guid LoanApplicationId, string RuleName, bool Passed, string Message, DateTime EvaluatedAt);
