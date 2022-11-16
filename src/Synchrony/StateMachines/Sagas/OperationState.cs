namespace Synchrony.StateMachines.Sagas;

using MassTransit;

public record OperationState :
    SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public Guid TransactionId { get; set; }
    public string Name { get; set; }
    public int SequenceNumber { get; set; }
    public int State { get; set; }
    public DateTimeOffset CreationTimestamp { get; set; }
}