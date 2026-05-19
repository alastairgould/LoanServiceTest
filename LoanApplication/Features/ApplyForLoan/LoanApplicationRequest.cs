namespace LoanApplication.Features.ApplyForLoan;

public record LoanApplicationRequest(string Name, string Email, int Amount, decimal MonthlyIncome, int TermMonths);