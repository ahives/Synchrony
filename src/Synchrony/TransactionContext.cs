namespace Synchrony;

public record TransactionContext
{
    public Guid TransactionId { get; init; }
    
    public TransactionState State { get; init; }
}