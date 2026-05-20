namespace LoanApplication.Domain;

public record LoanApplication(Guid Id, string Name, string Email, decimal MonthlyIncome, decimal RequestedAmount, int TermMonths, LoanStatus Status, DateTime CreatedAt, DateTime? ReviewedAt);