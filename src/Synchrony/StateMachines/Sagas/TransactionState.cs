namespace Synchrony.StateMachines.Sagas;

using MassTransit;

public record TransactionState :
    SagaStateMachineInstance
{
    public Guid Id { get; init; }
    public int State { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset CreationTimestamp { get; init; }
}