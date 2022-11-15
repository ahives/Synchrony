namespace Synchrony.StateMachines.Events;

public record TransactionCompleted
{
    public Guid TransactionId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}