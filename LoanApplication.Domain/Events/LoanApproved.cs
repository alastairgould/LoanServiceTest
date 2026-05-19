namespace LoanApplication.Domain.Events;

public record LoanApproved(Guid LoanApplicationId, DateTime ApprovedAt) : IEvent;
