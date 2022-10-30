namespace Synchrony;

public record OperationContext
{
    public Guid TransactionId { get; init; }
    
    public Guid OperationId { get; init; }
    
    public OperationState State { get; init; }
}