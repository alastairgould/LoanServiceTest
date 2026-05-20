namespace LoanApplication.Domain.Events;

public record LoanApproved(Guid Id, Guid LoanApplicationId, DateTime ApprovedAt) : IEvent;
