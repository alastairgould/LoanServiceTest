namespace LoanApplication.Features.ApplyForLoan;

public record LoanApplicationRequest(string Name, string Email, decimal MonthlyIncome, decimal RequestedAmount, int TermMonths);
