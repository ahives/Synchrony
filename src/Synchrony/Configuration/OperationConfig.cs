namespace Synchrony.Configuration;

public record OperationConfig
{
    public TransactionRetry TransactionRetry { get; init; }
    
    public bool Logging { get; init; }
}