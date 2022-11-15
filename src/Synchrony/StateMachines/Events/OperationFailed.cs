namespace Synchrony.StateMachines.Events;

public record OperationFailed
{
    public Guid OperationId { get; init; }
    public Guid TransactionId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}