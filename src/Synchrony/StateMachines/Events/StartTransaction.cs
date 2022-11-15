namespace Synchrony.StateMachines.Events;

public record StartTransaction
{
    public Guid TransactionId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}