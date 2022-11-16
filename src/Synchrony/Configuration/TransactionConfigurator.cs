namespace Synchrony.Configuration;

public interface TransactionConfigurator
{
    void TurnOnLogging();

    void Retry(TransactionRetry retry = TransactionRetry.None);

    void Subscribe(object observer, params object[] observers);
}