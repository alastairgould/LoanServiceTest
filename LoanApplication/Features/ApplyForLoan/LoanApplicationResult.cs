using LoanApplication.Domain;

namespace LoanApplication.Features.ApplyForLoan;

public record LoanApplicationResult(Guid Id, LoanStatus Status, DateTime CreatedAt);