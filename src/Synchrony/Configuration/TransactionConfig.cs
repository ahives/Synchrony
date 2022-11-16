namespace Synchrony.Configuration;

public record TransactionConfig
{
    public TransactionRetry TransactionRetry { get; init; }
    
    public List<IObserver<TransactionContext>> Subscribers { get; init; }
}