namespace Synchrony.StateMachines.Events;

public class StartOperation
{
    public Guid OperationId { get; init; }
    public Guid TransactionId { get; init; }
    public string Name { get; init; }
    public int SequenceNumber { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}