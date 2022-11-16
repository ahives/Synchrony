namespace Synchrony;

public record TransactionContext
{
    public Guid TransactionId { get; init; }
    
    public Guid OperationId { get; init; }
    
    public TransactionStates State { get; init; }
}