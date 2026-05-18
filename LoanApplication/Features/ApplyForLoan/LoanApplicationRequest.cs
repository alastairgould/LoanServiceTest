namespace LoanApplication.Features.ApplyForLoan;

public record LoanApplicationRequest(string Name, int Amount, decimal MonthlyIncome, int TermMonths);