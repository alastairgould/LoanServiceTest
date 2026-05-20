namespace LoanApplication.Domain.Events;

public record LoanRejected(Guid Id, Guid LoanApplicationId, DateTime RejectedAt) : IEvent;
