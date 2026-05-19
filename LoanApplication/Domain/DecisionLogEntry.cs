namespace LoanApplication.Domain;

public record DecisionLogEntry(Guid Id, Guid LoanApplicationId, string RuleName, bool Passed, string Message, DateTime EvaluatedAt);