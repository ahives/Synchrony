namespace Synchrony.StateMachines.Events;

public record TransactionFailed
{
    public Guid TransactionId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}