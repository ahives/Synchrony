namespace Synchrony.Configuration;

public interface TransactionConfigurator
{
    void TurnOnLogging();
    
    void TurnOnConsoleLogging();

    void Retry(TransactionRetry retry = TransactionRetry.None);

    void Subscribe(object observer, params object[] observers);

    void Subscribe(object observer);
}