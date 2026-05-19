namespace LoanApplication.Domain;

public record OutboxMessage(
    Guid Id,
    string Type,
    string Payload,
    DateTime OccurredAt,
    DateTime? PublishedAt);
