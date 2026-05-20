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
    DateTime? ReviewedAt);
