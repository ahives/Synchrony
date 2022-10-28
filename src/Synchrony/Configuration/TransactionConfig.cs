namespace Synchrony.Configuration;

public record TransactionConfig
{
    public bool ConsoleLoggingOn { get; init; }
    
    public TransactionRetry TransactionRetry { get; init; }
}