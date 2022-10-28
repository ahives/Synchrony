namespace Synchrony.Configuration;

public interface TransactionConfigurator
{
    void TurnOnLogging();
    
    void TurnOnConsoleLogging();

    void Retry(TransactionRetry retry = TransactionRetry.None);
}