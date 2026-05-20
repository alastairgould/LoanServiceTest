using LoanApplication.Domain;

namespace LoanApplication.Features.RetrieveLoanApplication;

public record LoanApplicationDetails(
    Guid Id,
    string Name,
    string Email,
    decimal MonthlyIncome,
    decimal RequestedAmount,
    int TermMonths,
    LoanStatus Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    IReadOnlyList<DecisionLogEntryDetails> DecisionLog);

public record DecisionLogEntryDetails(string RuleName, bool Passed, string Message, DateTime EvaluatedAt);
