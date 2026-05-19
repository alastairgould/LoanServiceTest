namespace LoanApplication.Domain.Events;

public record LoanRejected(Guid LoanApplicationId, DateTime RejectedAt) : IEvent;
